using Diwink.Extensions.EntityFrameworkCore.TestModel;
using Microsoft.EntityFrameworkCore;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Integration.Infrastructure;

/// <summary>
/// Handles schema creation and optional seed data initialization
/// for each test run against the containerized SQL Server.
/// </summary>
public static class DatabaseBootstrap
{
    /// <summary>
    /// Creates a fresh DbContext pointing at the container and ensures the schema exists.
    /// </summary>
    public static TestDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new TestDbContext(options);
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
