using System.Net;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Queries;
using StargateAPI.DataAccess;

namespace StargateAPI.Tests;

public class BusinessHandlerTests
{
    [Fact]
    public async Task CreatePersonHandlerCreatesPersonAndRejectsDuplicateName()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var handler = new CreatePersonHandler(context);

        var created = await handler.Handle(new CreatePerson { Name = "Mae Jemison" }, default);
        var duplicate = await handler.Handle(new CreatePerson { Name = "Mae Jemison" }, default);

        Assert.True(created.Success);
        Assert.True(created.Id > 0);
        Assert.False(duplicate.Success);
        Assert.Equal((int)HttpStatusCode.Conflict, duplicate.ResponseCode);
        Assert.Equal("A person named 'Mae Jemison' already exists.", duplicate.Message);
        Assert.Equal(1, await context.People.CountAsync());
    }

    [Fact]
    public async Task UpdatePersonHandlerUpdatesNameAndReportsMissingOrDuplicatePeople()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        context.People.AddRange(new Person { Name = "Mae Jemison" }, new Person { Name = "Sally Ride" });
        await context.SaveChangesAsync();
        var person = await context.People.SingleAsync(candidate => candidate.Name == "Mae Jemison");
        var handler = new UpdatePersonByIdHandler(context);

        var updated = await handler.Handle(
            new UpdatePersonById { PersonId = person.Id, NewName = "Dr. Mae Jemison" },
            default);
        var duplicate = await handler.Handle(
            new UpdatePersonById { PersonId = person.Id, NewName = "Sally Ride" },
            default);
        var missing = await handler.Handle(
            new UpdatePersonById { PersonId = int.MaxValue, NewName = "Nobody" },
            default);

        Assert.True(updated.Success);
        await context.Entry(person).ReloadAsync();
        Assert.Equal("Dr. Mae Jemison", person.Name);
        Assert.False(duplicate.Success);
        Assert.Equal((int)HttpStatusCode.Conflict, duplicate.ResponseCode);
        Assert.False(missing.Success);
        Assert.Equal((int)HttpStatusCode.NotFound, missing.ResponseCode);
    }

    [Fact]
    public async Task CreateAstronautDutyHandlerMaintainsCareerAndDutyHistory()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var person = new Person { Name = "Mae Jemison" };
        context.People.Add(person);
        await context.SaveChangesAsync();
        var handler = new CreateAstronautDutyHandler(context);
        var firstStart = new DateTime(2024, 1, 10, 15, 30, 0, DateTimeKind.Utc);

        var firstDuty = await handler.Handle(
            new CreateAstronautDuty
            {
                Name = person.Name,
                Rank = "Captain",
                DutyTitle = "Commander",
                DutyStartDate = firstStart
            },
            default);
        var retirementDate = new DateTime(2025, 6, 5, 8, 0, 0, DateTimeKind.Utc);
        var retirement = await handler.Handle(
            new CreateAstronautDuty
            {
                Name = person.Name,
                Rank = "Captain",
                DutyTitle = "RETIRED",
                DutyStartDate = retirementDate
            },
            default);

        var duties = await context.AstronautDuties.OrderBy(duty => duty.DutyStartDate).ToListAsync();
        var details = await context.AstronautDetails.SingleAsync();
        Assert.True(firstDuty.Success);
        Assert.True(retirement.Success);
        Assert.Equal(2, duties.Count);
        Assert.Equal(firstStart.Date, duties[0].DutyStartDate);
        Assert.Equal(retirementDate.Date.AddDays(-1), duties[0].DutyEndDate);
        Assert.Null(duties[1].DutyEndDate);
        Assert.Equal("RETIRED", details.CurrentDutyTitle);
        Assert.Equal(retirementDate.Date.AddDays(-1), details.CareerEndDate);
        Assert.Equal(firstStart.Date, details.CareerStartDate);
    }

    [Fact]
    public async Task CreateAstronautDutyHandlerRejectsMissingPersonAndNonChronologicalDuty()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var person = new Person { Name = "Mae Jemison" };
        context.People.Add(person);
        await context.SaveChangesAsync();
        var handler = new CreateAstronautDutyHandler(context);
        var dutyDate = new DateTime(2024, 1, 10);
        var initial = new CreateAstronautDuty
        {
            Name = person.Name,
            Rank = "Captain",
            DutyTitle = "Commander",
            DutyStartDate = dutyDate
        };
        await handler.Handle(initial, default);

        var duplicateDate = await handler.Handle(initial, default);
        var unknownPerson = await handler.Handle(
            new CreateAstronautDuty
            {
                Name = "Unknown",
                Rank = "Captain",
                DutyTitle = "Commander",
                DutyStartDate = dutyDate.AddDays(1)
            },
            default);

        Assert.False(duplicateDate.Success);
        Assert.Equal((int)HttpStatusCode.BadRequest, duplicateDate.ResponseCode);
        Assert.False(unknownPerson.Success);
        Assert.Equal((int)HttpStatusCode.NotFound, unknownPerson.ResponseCode);
        Assert.Single(await context.AstronautDuties.ToListAsync());
    }

    [Fact]
    public async Task GetPersonByNameHandlerReturnsPersonOrNotFound()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        context.People.Add(new Person { Name = "Mae Jemison" });
        await context.SaveChangesAsync();
        var handler = new GetPersonByNameHandler(context);

        var found = await handler.Handle(new GetPersonByName { Name = "Mae Jemison" }, default);
        var missing = await handler.Handle(new GetPersonByName { Name = "Unknown" }, default);

        Assert.True(found.Success);
        Assert.Equal("Mae Jemison", found.Person?.Name);
        Assert.Equal(string.Empty, found.Person?.CurrentRank);
        Assert.False(missing.Success);
        Assert.Equal((int)HttpStatusCode.NotFound, missing.ResponseCode);
    }

    [Fact]
    public async Task GetPeopleHandlerReturnsPeopleIncludingNonAstronauts()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var astronaut = new Person { Name = "Mae Jemison" };
        context.People.AddRange(astronaut, new Person { Name = "Sally Ride" });
        await context.SaveChangesAsync();
        context.AstronautDetails.Add(new AstronautDetail
        {
            PersonId = astronaut.Id,
            CurrentRank = "Captain",
            CurrentDutyTitle = "Commander",
            CareerStartDate = new DateTime(2024, 1, 1)
        });
        await context.SaveChangesAsync();

        var result = await new GetPeopleHandler(context).Handle(new GetPeople(), default);

        Assert.True(result.Success);
        Assert.Equal(2, result.People.Count);
        Assert.Contains(result.People, person => person.Name == "Mae Jemison" && person.CurrentRank == "Captain");
        Assert.Contains(result.People, person => person.Name == "Sally Ride");
    }

    [Fact]
    public async Task GetAstronautDutiesByNameHandlerReturnsHistoryInReverseChronologicalOrder()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var person = new Person { Name = "Mae Jemison" };
        context.People.Add(person);
        await context.SaveChangesAsync();
        context.AstronautDuties.AddRange(
            new AstronautDuty
            {
                PersonId = person.Id,
                Rank = "Captain",
                DutyTitle = "Commander",
                DutyStartDate = new DateTime(2024, 1, 1)
            },
            new AstronautDuty
            {
                PersonId = person.Id,
                Rank = "Captain",
                DutyTitle = "RETIRED",
                DutyStartDate = new DateTime(2025, 1, 1)
            });
        await context.SaveChangesAsync();

        var found = await new GetAstronautDutiesByNameHandler(context)
            .Handle(new GetAstronautDutiesByName { Name = person.Name }, default);
        var missing = await new GetAstronautDutiesByNameHandler(context)
            .Handle(new GetAstronautDutiesByName { Name = "Unknown" }, default);

        Assert.True(found.Success);
        Assert.Equal("Mae Jemison", found.Person?.Name);
        Assert.Equal(new[] { "RETIRED", "Commander" }, found.AstronautDuties.Select(duty => duty.DutyTitle));
        Assert.False(missing.Success);
        Assert.Equal((int)HttpStatusCode.NotFound, missing.ResponseCode);
    }
}
