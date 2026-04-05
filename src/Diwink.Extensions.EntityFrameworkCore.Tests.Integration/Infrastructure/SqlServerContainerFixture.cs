using Testcontainers.MsSql;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Integration.Infrastructure;

/// <summary>
/// xUnit collection fixture that manages a SQL Server container lifecycle
/// shared across all integration tests in a collection.
/// </summary>
public class SqlServerContainerFixture : IAsyncLifetime
{
    internal const string DefaultSqlServerImage = "mcr.microsoft.com/mssql/server:2022-latest";

    private readonly MsSqlContainer _container = new MsSqlBuilder(GetSqlServerImage())
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Starts the SQL Server container used by the fixture.
    /// </summary>
    /// <returns>A task that completes when the container has started.</returns>
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    /// <summary>
    /// Disposes the SQL Server container and releases its resources.
    /// </summary>
    /// <returns>A task that completes when the container has been disposed.</returns>
    public async Task DisposeAsync()
    {
        await _container.DisposeAsync().AsTask();
    }

    private static string GetSqlServerImage()
    {
        var configuredImage = Environment.GetEnvironmentVariable("SQL_SERVER_IMAGE");
        return string.IsNullOrWhiteSpace(configuredImage)
            ? DefaultSqlServerImage
            : configuredImage.Trim();
    }
}
