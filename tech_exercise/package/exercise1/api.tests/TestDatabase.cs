using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StargateAPI.DataAccess;

namespace StargateAPI.Tests;

internal sealed class TestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private TestDatabase(SqliteConnection connection, DbContextOptions<StargateContext> options)
    {
        _connection = connection;
        Options = options;
        Factory = new TestContextFactory(options);
    }

    public DbContextOptions<StargateContext> Options { get; }

    public IDbContextFactory<StargateContext> Factory { get; }

    public static async Task<TestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<StargateContext>()
            .UseSqlite(connection)
            .Options;
        var database = new TestDatabase(connection, options);

        await using var context = database.CreateContext();
        await context.Database.EnsureCreatedAsync();

        return database;
    }

    public StargateContext CreateContext() => new(Options);

    public ValueTask DisposeAsync() => _connection.DisposeAsync();

    private sealed class TestContextFactory(DbContextOptions<StargateContext> options)
        : IDbContextFactory<StargateContext>
    {
        public StargateContext CreateDbContext() => new(options);

        public Task<StargateContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(CreateDbContext());
        }
    }
}
