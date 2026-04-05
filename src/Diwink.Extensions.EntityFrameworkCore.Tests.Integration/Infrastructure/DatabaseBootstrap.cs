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
        await using var context = CreateContext(connectionString);
        await context.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Drops and recreates the database schema. Used for test isolation
    /// when a test needs a guaranteed clean slate.
    /// </summary>
    public static async Task ResetSchemaAsync(string connectionString)
    {
        await using var context = CreateContext(connectionString);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }
}
