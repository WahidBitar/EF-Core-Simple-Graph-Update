using Diwink.Extensions.EntityFrameworkCore.V2.TestModel;

namespace Diwink.Extensions.EntityFrameworkCore.V2.Tests.Integration.Infrastructure;

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

    protected IntegrationTestBase(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public virtual async Task InitializeAsync()
    {
        await DatabaseBootstrap.ResetSchemaAsync(ConnectionString);
    }

    public virtual Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Creates a fresh DbContext for the current test.
    /// </summary>
    protected V2TestDbContext CreateContext()
    {
        return DatabaseBootstrap.CreateContext(ConnectionString);
    }
}
