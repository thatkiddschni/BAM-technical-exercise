using MediatR;
using StargateAPI.Business;
using StargateAPI.Business.Commands;
using StargateAPI.Controllers;
using StargateAPI.Services;

namespace StargateAPI.Tests;

public class ProcessLoggingBehaviorTests
{
    [Theory]
    [InlineData(true, "Success")]
    [InlineData(false, "Failure")]
    public async Task HandleLogsOutcomeFromResponse(bool succeeded, string expectedOutcome)
    {
        await using var database = await TestDatabase.CreateAsync();
        var logger = new ProcessLogger(database.Factory);
        var behavior = new ProcessLoggingBehavior<CreatePerson, BaseResponse>(logger);
        var response = new BaseResponse
        {
            Success = succeeded,
            Message = "Result"
        };
        RequestHandlerDelegate<BaseResponse> next = () => Task.FromResult(response);

        var actual = await behavior.Handle(new CreatePerson { Name = "Mae Jemison" }, next, default);
        var logs = await logger.GetAllAsync();

        Assert.Same(response, actual);
        var log = Assert.Single(logs);
        Assert.Equal(expectedOutcome, log.Outcome);
        Assert.Equal("CreatePerson", log.Operation);
        Assert.Equal("Result", log.Message);
    }

    [Fact]
    public async Task HandleLogsAndRethrowsHandlerException()
    {
        await using var database = await TestDatabase.CreateAsync();
        var logger = new ProcessLogger(database.Factory);
        var behavior = new ProcessLoggingBehavior<CreatePerson, BaseResponse>(logger);
        var exception = new InvalidOperationException("Handler failed.");
        RequestHandlerDelegate<BaseResponse> next = () => Task.FromException<BaseResponse>(exception);

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => behavior.Handle(new CreatePerson { Name = "Mae Jemison" }, next, default));
        var log = Assert.Single(await logger.GetAllAsync());

        Assert.Same(exception, thrown);
        Assert.Equal("Exception", log.Outcome);
        Assert.Equal(exception.Message, log.Message);
        Assert.Equal(typeof(InvalidOperationException).FullName, log.ExceptionType);
    }
}
