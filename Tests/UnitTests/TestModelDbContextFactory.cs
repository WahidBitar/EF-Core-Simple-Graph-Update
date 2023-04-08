using Microsoft.EntityFrameworkCore.Design;


namespace UnitTests;

public class TestModelDbContextFactory : IDesignTimeDbContextFactory<TestModelDbContext>
{
    public TestModelDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        
        var optionsBuilder = new DbContextOptionsBuilder<TestModelDbContext>()
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging()
            .UseSqlServer(configuration.GetConnectionString("TestModelDb"));

        return new TestModelDbContext(optionsBuilder.Options);
    }
}