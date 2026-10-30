using System.Net;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Moq;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Queries;
using StargateAPI.Controllers;
using StargateAPI.DataAccess;
using StargateAPI.Services;

namespace StargateAPI.Tests;

public class ControllerTests
{
    [Fact]
    public async Task PersonControllerRejectsInvalidNameWithoutCallingMediator()
    {
        await using var database = await TestDatabase.CreateAsync();
        var mediator = new Mock<IMediator>();
        var controller = CreateController(new PersonController(mediator.Object, new ProcessLogger(database.Factory)),
            nameof(PersonController.GetPersonByName));

        var result = await controller.GetPersonByName(new string('a', 51));

        AssertResponse(result, HttpStatusCode.BadRequest, success: false);
        mediator.Verify(m => m.Send(It.IsAny<GetPersonByName>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PersonControllerReturnsSuccessfulPersonResponse()
    {
        await using var database = await TestDatabase.CreateAsync();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetPersonByName>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetPersonByNameResult
            {
                Success = true,
                Person = new() { PersonId = 7, Name = "Mae Jemison" }
            });
        var controller = CreateController(new PersonController(mediator.Object, new ProcessLogger(database.Factory)),
            nameof(PersonController.GetPersonByName));

        var result = await controller.GetPersonByName("Mae Jemison");

        var response = AssertResponse(result, HttpStatusCode.OK, success: true);
        Assert.Equal("Successful", response.Message);
        mediator.Verify(m => m.Send(
            It.Is<GetPersonByName>(request => request.Name == "Mae Jemison"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PersonControllerReturnsPeopleAndNotFoundResults()
    {
        await using var database = await TestDatabase.CreateAsync();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetPeople>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetPeopleResult { Success = true });
        mediator.Setup(m => m.Send(It.IsAny<GetPersonByName>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetPersonByNameResult
            {
                Success = false,
                ResponseCode = (int)HttpStatusCode.NotFound,
                Message = "Not found."
            });
        var controller = CreateController(new PersonController(mediator.Object, new ProcessLogger(database.Factory)),
            nameof(PersonController.GetPeople));

        var peopleResult = await controller.GetPeople();
        controller.ControllerContext.ActionDescriptor.ActionName = nameof(PersonController.GetPersonByName);
        var missingPersonResult = await controller.GetPersonByName("Unknown");

        AssertResponse(peopleResult, HttpStatusCode.OK, success: true);
        AssertResponse(missingPersonResult, HttpStatusCode.NotFound, success: false);
    }

    [Fact]
    public async Task PersonControllerCreatesAndUpdatesPeople()
    {
        await using var database = await TestDatabase.CreateAsync();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<CreatePerson>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreatePersonResult { Success = true, Id = 7 });
        mediator.Setup(m => m.Send(It.IsAny<UpdatePersonById>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdatePersonByIdResult { Success = true, Id = 7 });
        var controller = CreateController(new PersonController(mediator.Object, new ProcessLogger(database.Factory)),
            nameof(PersonController.CreatePerson));

        var createResult = await controller.CreatePerson("Mae Jemison");
        controller.ControllerContext.ActionDescriptor.ActionName = nameof(PersonController.UpdatePersonById);
        var updateResult = await controller.UpdatePersonById(7, "Dr. Mae Jemison");

        AssertResponse(createResult, HttpStatusCode.OK, success: true);
        AssertResponse(updateResult, HttpStatusCode.OK, success: true);
        mediator.Verify(m => m.Send(
            It.Is<CreatePerson>(request => request.Name == "Mae Jemison"),
            It.IsAny<CancellationToken>()), Times.Once);
        mediator.Verify(m => m.Send(
            It.Is<UpdatePersonById>(request => request.PersonId == 7 && request.NewName == "Dr. Mae Jemison"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PersonControllerRejectsInvalidUpdateWithoutCallingMediator()
    {
        await using var database = await TestDatabase.CreateAsync();
        var mediator = new Mock<IMediator>();
        var controller = CreateController(new PersonController(mediator.Object, new ProcessLogger(database.Factory)),
            nameof(PersonController.UpdatePersonById));

        var result = await controller.UpdatePersonById(0, "Mae Jemison");

        AssertResponse(result, HttpStatusCode.BadRequest, success: false);
        mediator.Verify(m => m.Send(It.IsAny<UpdatePersonById>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PersonControllerRejectsEmptyQueriesAndInvalidUpdateNames()
    {
        await using var database = await TestDatabase.CreateAsync();
        var mediator = new Mock<IMediator>();
        var controller = CreateController(new PersonController(mediator.Object, new ProcessLogger(database.Factory)),
            nameof(PersonController.GetPersonByName));

        var emptyQuery = await controller.GetPersonByName(string.Empty);
        controller.ControllerContext.ActionDescriptor.ActionName = nameof(PersonController.UpdatePersonById);
        var emptyUpdate = await controller.UpdatePersonById(1, string.Empty);
        var longUpdate = await controller.UpdatePersonById(1, new string('a', 51));

        AssertResponse(emptyQuery, HttpStatusCode.BadRequest, success: false);
        AssertResponse(emptyUpdate, HttpStatusCode.BadRequest, success: false);
        AssertResponse(longUpdate, HttpStatusCode.BadRequest, success: false);
        mediator.Verify(m => m.Send(It.IsAny<GetPersonByName>(), It.IsAny<CancellationToken>()), Times.Never);
        mediator.Verify(m => m.Send(It.IsAny<UpdatePersonById>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PersonControllerMapsMediatorExceptionsToServerErrorsAndLogsThem()
    {
        await using var database = await TestDatabase.CreateAsync();
        var logger = new ProcessLogger(database.Factory);
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetPeople>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unavailable"));
        var controller = CreateController(new PersonController(mediator.Object, logger),
            nameof(PersonController.GetPeople));

        var result = await controller.GetPeople();

        var response = AssertResponse(result, HttpStatusCode.InternalServerError, success: false);
        Assert.Equal("Unavailable", response.Message);
        var logs = await logger.GetAllAsync();
        Assert.Contains(logs, log => log.Outcome == "Exception");
        Assert.Contains(logs, log => log.Outcome == "Failure");
    }

    [Fact]
    public async Task AstronautDutyControllerRejectsMissingRequest()
    {
        await using var database = await TestDatabase.CreateAsync();
        var mediator = new Mock<IMediator>();
        var controller = CreateController(
            new AstronautDutyController(mediator.Object, new ProcessLogger(database.Factory)),
            nameof(AstronautDutyController.CreateAstronautDuty));

        var result = await controller.CreateAstronautDuty(null);

        AssertResponse(result, HttpStatusCode.BadRequest, success: false);
        mediator.Verify(m => m.Send(It.IsAny<CreateAstronautDuty>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AstronautDutyControllerReturnsSuccessfulCreationResponse()
    {
        await using var database = await TestDatabase.CreateAsync();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<CreateAstronautDuty>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateAstronautDutyResult { Success = true, Id = 12 });
        var controller = CreateController(
            new AstronautDutyController(mediator.Object, new ProcessLogger(database.Factory)),
            nameof(AstronautDutyController.CreateAstronautDuty));

        var result = await controller.CreateAstronautDuty(new CreateAstronautDuty
        {
            Name = "Mae Jemison",
            Rank = "Captain",
            DutyTitle = "Commander",
            DutyStartDate = new DateTime(2024, 1, 1)
        });

        AssertResponse(result, HttpStatusCode.OK, success: true);
    }

    [Fact]
    public async Task AstronautDutyControllerReturnsDutiesOrNotFound()
    {
        await using var database = await TestDatabase.CreateAsync();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetAstronautDutiesByName>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetAstronautDutiesByName request, CancellationToken _) =>
                request.Name == "Mae Jemison"
                    ? new GetAstronautDutiesByNameResult
                    {
                        Success = true,
                        Person = new() { PersonId = 7, Name = request.Name },
                        AstronautDuties =
                        [
                            new AstronautDuty
                            {
                                PersonId = 7,
                                Rank = "Captain",
                                DutyTitle = "Commander",
                                DutyStartDate = new DateTime(2024, 1, 1)
                            }
                        ]
                    }
                    : new GetAstronautDutiesByNameResult
                    {
                        Success = false,
                        ResponseCode = (int)HttpStatusCode.NotFound,
                        Message = "Missing person."
                    });
        var controller = CreateController(
            new AstronautDutyController(mediator.Object, new ProcessLogger(database.Factory)),
            nameof(AstronautDutyController.GetAstronautDutiesByName));

        var found = await controller.GetAstronautDutiesByName("Mae Jemison");
        var missing = await controller.GetAstronautDutiesByName("Unknown");

        AssertResponse(found, HttpStatusCode.OK, success: true);
        AssertResponse(missing, HttpStatusCode.NotFound, success: false);
    }

    [Fact]
    public async Task AstronautDutyControllerReturnsNotFoundWhenPersonHasNoDuties()
    {
        await using var database = await TestDatabase.CreateAsync();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetAstronautDutiesByName>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetAstronautDutiesByNameResult
            {
                Success = true,
                Person = new() { PersonId = 7, Name = "Mae Jemison" }
            });
        var controller = CreateController(
            new AstronautDutyController(mediator.Object, new ProcessLogger(database.Factory)),
            nameof(AstronautDutyController.GetAstronautDutiesByName));

        var result = await controller.GetAstronautDutiesByName("Mae Jemison");

        AssertResponse(result, HttpStatusCode.NotFound, success: false);
    }

    [Fact]
    public async Task AstronautDutyControllerRejectsInvalidNamesAndDutyFields()
    {
        await using var database = await TestDatabase.CreateAsync();
        var mediator = new Mock<IMediator>();
        var controller = CreateController(
            new AstronautDutyController(mediator.Object, new ProcessLogger(database.Factory)),
            nameof(AstronautDutyController.GetAstronautDutiesByName));

        var emptyName = await controller.GetAstronautDutiesByName(string.Empty);
        var longName = await controller.GetAstronautDutiesByName(new string('a', 51));
        controller.ControllerContext.ActionDescriptor.ActionName = nameof(AstronautDutyController.CreateAstronautDuty);
        var emptyNameRequest = await controller.CreateAstronautDuty(new CreateAstronautDuty
        {
            Name = " ",
            Rank = "Captain",
            DutyTitle = "Commander",
            DutyStartDate = new DateTime(2024, 1, 1)
        });
        var emptyRankRequest = await controller.CreateAstronautDuty(new CreateAstronautDuty
        {
            Name = "Mae Jemison",
            Rank = " ",
            DutyTitle = "Commander",
            DutyStartDate = new DateTime(2024, 1, 1)
        });
        var missingDateRequest = await controller.CreateAstronautDuty(new CreateAstronautDuty
        {
            Name = "Mae Jemison",
            Rank = "Captain",
            DutyTitle = "Commander",
            DutyStartDate = default
        });

        AssertResponse(emptyName, HttpStatusCode.BadRequest, success: false);
        AssertResponse(longName, HttpStatusCode.BadRequest, success: false);
        AssertResponse(emptyNameRequest, HttpStatusCode.BadRequest, success: false);
        AssertResponse(emptyRankRequest, HttpStatusCode.BadRequest, success: false);
        AssertResponse(missingDateRequest, HttpStatusCode.BadRequest, success: false);
        mediator.Verify(m => m.Send(It.IsAny<GetAstronautDutiesByName>(), It.IsAny<CancellationToken>()), Times.Never);
        mediator.Verify(m => m.Send(It.IsAny<CreateAstronautDuty>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessLoggerControllerReturnsPersistedLogs()
    {
        await using var database = await TestDatabase.CreateAsync();
        var logger = new ProcessLogger(database.Factory);
        await logger.LogAsync("GetPeople", "PersonController", "Success", "Successful");
        var controller = new ProcessLoggerController(logger);

        var result = await controller.GetAllLogs(default);

        var logs = Assert.IsType<OkObjectResult>(result.Result);
        var entries = Assert.IsAssignableFrom<IReadOnlyList<ProcessLog>>(logs.Value);
        Assert.Single(entries);
        Assert.Equal("GetPeople", entries[0].Operation);
    }

    private static TController CreateController<TController>(TController controller, string actionName)
        where TController : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            ActionDescriptor = new ControllerActionDescriptor { ActionName = actionName }
        };
        return controller;
    }

    private static BaseResponse AssertResponse(IActionResult actionResult, HttpStatusCode statusCode, bool success)
    {
        var result = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal((int)statusCode, result.StatusCode);
        var response = Assert.IsAssignableFrom<BaseResponse>(result.Value);
        Assert.Equal(success, response.Success);
        return response;
    }
}
