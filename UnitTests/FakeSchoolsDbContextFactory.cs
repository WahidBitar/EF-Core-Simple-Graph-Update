using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;


namespace UnitTests
{
    public class FakeSchoolsDbContextFactory : IDesignTimeDbContextFactory<FakeSchoolsDbContext>
    {
        public FakeSchoolsDbContext CreateDbContext(string[] args)
        {
            var configuration = TestHelpers.InitConfiguration();

            var optionsBuilder = new DbContextOptionsBuilder<FakeSchoolsDbContext>()
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging()
                .UseSqlServer(configuration.GetConnectionString("FakeSchoolsDb"));

            return new FakeSchoolsDbContext(optionsBuilder.Options);
        }
    }
}