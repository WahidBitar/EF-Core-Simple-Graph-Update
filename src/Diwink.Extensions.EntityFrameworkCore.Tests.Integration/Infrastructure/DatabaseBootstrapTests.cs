using FluentAssertions;
using Microsoft.Data.SqlClient;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Integration.Infrastructure;

public class DatabaseBootstrapTests
{
    [Fact]
    public void GetTestConnectionString_replaces_master_with_dedicated_test_database()
    {
        var connectionString = "Server=localhost,1433;Database=master;User ID=sa;Password=Passw0rd!;";

        var normalized = DatabaseBootstrap.GetTestConnectionString(connectionString);
        var builder = new SqlConnectionStringBuilder(normalized);

        builder.InitialCatalog.Should().Be(DatabaseBootstrap.TestDatabaseName);
    }

    [Fact]
    public void GetTestConnectionString_preserves_existing_non_master_database()
    {
        var connectionString = "Server=localhost,1433;Database=CustomDb;User ID=sa;Password=Passw0rd!;";

        var normalized = DatabaseBootstrap.GetTestConnectionString(connectionString);
        var builder = new SqlConnectionStringBuilder(normalized);

        builder.InitialCatalog.Should().Be("CustomDb");
    }
}
