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
    /// </summary>
    public static TestDbContext CreateContext(string connectionString)
    {
        var testConnectionString = GetTestConnectionString(connectionString);
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer(testConnectionString)
            .Options;

        return new TestDbContext(options);
    }

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
    /// </summary>
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
    /// </summary>
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
