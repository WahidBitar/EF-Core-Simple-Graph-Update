using Diwink.Extensions.EntityFrameworkCore.TestModel;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Integration.Infrastructure;

/// <summary>
/// Handles schema creation and optional seed data initialization
/// for each test run against the containerized SQL Server.
/// </summary>
public static class DatabaseBootstrap
{
    internal const string TestDatabaseName = "DiwinkEfCoreGraphUpdateTests";
    private const int SchemaOperationMaxAttempts = 3;
    private static readonly TimeSpan SchemaOperationTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan SchemaOperationRetryDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Creates a fresh DbContext pointing at the container and ensures the schema exists.
    /// <summary>
    /// Creates a TestDbContext configured to use SQL Server with a test-specific connection string.
    /// </summary>
    /// <param name="connectionString">Base SQL Server connection string; if its initial catalog is empty or equals "master", the catalog will be replaced with the test database name.</param>
    /// <returns>A new TestDbContext configured with the adjusted connection string.</returns>
    public static TestDbContext CreateContext(string connectionString)
    {
        var testConnectionString = GetTestConnectionString(connectionString);
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer(testConnectionString)
            .Options;

        return new TestDbContext(options);
    }

    /// <summary>
    /// Ensures the provided SQL Server connection string targets the test database by replacing an empty or "master" Initial Catalog with the test database name.
    /// </summary>
    /// <param name="connectionString">A SQL Server connection string to adjust.</param>
    /// <returns>The connection string with Initial Catalog set to the test database when the original catalog was empty or "master".</returns>
    internal static string GetTestConnectionString(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);

        if (string.IsNullOrWhiteSpace(builder.InitialCatalog) ||
            string.Equals(builder.InitialCatalog, "master", StringComparison.OrdinalIgnoreCase))
        {
            builder.InitialCatalog = TestDatabaseName;
        }

        return builder.ConnectionString;
    }

    /// <summary>
    /// Ensures the database schema is created. Called once per test collection.
    /// <summary>
    /// Ensures the test database schema exists for the provided SQL Server connection string.
    /// </summary>
    /// <param name="connectionString">SQL Server connection string. If the Initial Catalog is empty or "master", the configured test database name will be used.</param>
    public static async Task EnsureSchemaAsync(string connectionString)
    {
        await ExecuteSchemaOperationWithRetryAsync(
            connectionString,
            "ensure the test schema exists",
            static (context, cancellationToken) => context.Database.EnsureCreatedAsync(cancellationToken));
    }

    /// <summary>
    /// Drops and recreates the database schema. Used for test isolation
    /// when a test needs a guaranteed clean slate.
    /// <summary>
    /// Deletes and recreates the test database schema for the given connection string.
    /// </summary>
    /// <param name="connectionString">The database connection string used to locate the server; if the connection string has no catalog or specifies "master", the configured test database name will be used.</param>
    public static async Task ResetSchemaAsync(string connectionString)
    {
        await ExecuteSchemaOperationWithRetryAsync(
            connectionString,
            "reset the test schema",
            static async (context, cancellationToken) =>
            {
                await context.Database.EnsureDeletedAsync(cancellationToken);
                await context.Database.EnsureCreatedAsync(cancellationToken);
            });
    }

    private static async Task ExecuteSchemaOperationWithRetryAsync(
        string connectionString,
        string operationName,
        Func<TestDbContext, CancellationToken, Task> operation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        Exception? lastException = null;

        for (var attempt = 1; attempt <= SchemaOperationMaxAttempts; attempt++)
        {
            using var cancellationSource = new CancellationTokenSource(SchemaOperationTimeout);

            try
            {
                await using var context = CreateContext(connectionString);
                await operation(context, cancellationSource.Token);
                return;
            }
            catch (Exception ex) when (IsRetryableSchemaException(ex, cancellationSource))
            {
                lastException = ex;

                if (attempt == SchemaOperationMaxAttempts)
                    break;

                await Task.Delay(SchemaOperationRetryDelay);
            }
        }

        throw new InvalidOperationException(
            $"Failed to {operationName} after {SchemaOperationMaxAttempts} attempts.",
            lastException);
    }

    private static bool IsRetryableSchemaException(Exception exception, CancellationTokenSource cancellationSource)
    {
        return exception is SqlException or TimeoutException or InvalidOperationException ||
               (cancellationSource.IsCancellationRequested && exception is OperationCanceledException);
    }
}
