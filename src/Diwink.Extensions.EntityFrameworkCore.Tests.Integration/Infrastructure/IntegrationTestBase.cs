using Diwink.Extensions.EntityFrameworkCore.TestModel;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Integration.Infrastructure;

/// <summary>
/// Base class for integration tests. Provides per-test database isolation
/// by resetting the schema before each test, and a factory for creating
/// DbContext instances connected to the container.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly SqlServerContainerFixture _fixture;

    protected string ConnectionString => _fixture.ConnectionString;

    /// <summary>
    /// Initializes a new instance of the IntegrationTestBase class with the provided SQL Server container fixture.
    /// </summary>
    /// <param name="fixture">The SQL Server container fixture used to obtain the test database connection for each test.</param>
    protected IntegrationTestBase(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Resets the test database schema prior to running a test.
    /// </summary>
    /// <returns>A task that completes when the schema reset operation has finished.</returns>
    public virtual async Task InitializeAsync()
    {
        await DatabaseBootstrap.ResetSchemaAsync(ConnectionString);
    }

    /// <summary>
/// Hook invoked after a test finishes to perform any necessary cleanup; no action is taken by default.
/// </summary>
/// <returns>A Task that completes when cleanup is finished.</returns>
public virtual Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Creates a fresh DbContext for the current test.
    /// <summary>
    /// Creates a new TestDbContext configured for the integration test database.
    /// </summary>
    /// <returns>The new TestDbContext instance connected to the test database.</returns>
    protected TestDbContext CreateContext()
    {
        return DatabaseBootstrap.CreateContext(ConnectionString);
    }
}
