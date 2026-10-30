using StargateAPI.DataAccess;
using StargateAPI.Services;

namespace StargateAPI.Tests;

public class ServiceTests
{
    [Fact]
    public async Task ProcessLoggerPersistsEntriesAndExceptionDetailsInIdOrder()
    {
        await using var database = await TestDatabase.CreateAsync();
        var logger = new ProcessLogger(database.Factory);
        var exception = new InvalidOperationException("The operation failed.");
        var earliestExpectedTime = DateTime.UtcNow;

        await logger.LogAsync("CreatePerson", "PersonController", "Exception", exception.Message, exception);
        await logger.LogAsync("GetPeople", "PersonController", "Success", "Successful");
        var latestExpectedTime = DateTime.UtcNow;
        var logs = await logger.GetAllAsync();

        Assert.Equal(2, logs.Count);
        Assert.Equal("Exception", logs[0].Outcome);
        Assert.Equal(typeof(InvalidOperationException).FullName, logs[0].ExceptionType);
        Assert.Contains(exception.Message, logs[0].ExceptionDetails);
        Assert.Null(logs[1].ExceptionType);
        Assert.Null(logs[1].ExceptionDetails);
        Assert.All(logs, log => Assert.InRange(log.OccurredAtUtc, earliestExpectedTime, latestExpectedTime));
    }
}
