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

    [Fact]
    public void GetTestConnectionString_replaces_MASTER_uppercase_with_test_database()
    {
        var connectionString = "Server=localhost,1433;Database=MASTER;User ID=sa;Password=Passw0rd!;";

        var normalized = DatabaseBootstrap.GetTestConnectionString(connectionString);
        var builder = new SqlConnectionStringBuilder(normalized);

        builder.InitialCatalog.Should().Be(DatabaseBootstrap.TestDatabaseName);
    }

    [Fact]
    public void GetTestConnectionString_replaces_mixed_case_master_with_test_database()
    {
        var connectionString = "Server=localhost,1433;Database=MaStEr;User ID=sa;Password=Passw0rd!;";

        var normalized = DatabaseBootstrap.GetTestConnectionString(connectionString);
        var builder = new SqlConnectionStringBuilder(normalized);

        builder.InitialCatalog.Should().Be(DatabaseBootstrap.TestDatabaseName);
    }

    [Fact]
    public void GetTestConnectionString_when_no_database_key_sets_test_database()
    {
        // Connection string without an explicit Database/Initial Catalog key
        var connectionString = "Server=localhost,1433;User ID=sa;Password=Passw0rd!;";

        var normalized = DatabaseBootstrap.GetTestConnectionString(connectionString);
        var builder = new SqlConnectionStringBuilder(normalized);

        builder.InitialCatalog.Should().Be(DatabaseBootstrap.TestDatabaseName);
    }

    [Fact]
    public void TestDatabaseName_is_not_master_or_empty()
    {
        DatabaseBootstrap.TestDatabaseName.Should().NotBeNullOrWhiteSpace();
        DatabaseBootstrap.TestDatabaseName.Should().NotBeEquivalentTo("master");
    }
}