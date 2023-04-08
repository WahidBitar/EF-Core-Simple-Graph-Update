namespace UnitTests.Tests;

[TestCaseOrderer("UnitTests.PriorityOrderer", "UnitTests")]
public class TestDbFixture : IDisposable
{
    public readonly ServiceProvider ServiceProvider;
    private bool inMemoryDb;

    public TestDbFixture()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var services = new ServiceCollection();

        services.AddDbContext<TestModelDbContext>(options =>
            {
                options.EnableDetailedErrors().EnableSensitiveDataLogging();
                inMemoryDb = false;
                bool.TryParse(configuration["InMemoryDB"], out inMemoryDb);
                if (inMemoryDb)
                    options.UseInMemoryDatabase("TestModelDb");
                else
                    options.UseSqlServer(configuration.GetConnectionString("TestModelDb"));
            }
        );

        ServiceProvider = services.BuildServiceProvider();
        using var initialScope = ServiceProvider.CreateScope();
        var initialDbContext = initialScope.ServiceProvider.GetRequiredService<TestModelDbContext>();
        initialDbContext.Database.EnsureDeleted();
        initialDbContext.Database.EnsureCreated();
        seedInitialData(initialDbContext);
        initialDbContext.SaveChanges();
        initialDbContext.Dispose();
    }

    private void seedInitialData(TestModelDbContext dbContext)
    {
        var aggregateA = new AggregateA
        {
            RequiredText = "AggregateA",
            CompositeEntities = new List<CompositeEntity>
            {
                new()
                {
                    RequiredText = "AggregateA Comp1",
                    SomeId = 1,
                    AggregateAId = 0,
                },
                new()
                {
                    RequiredText = "AggregateA Comp2",
                    SomeId = 2,
                    AggregateAId = 0,
                }
            },
        };
        dbContext.AggregateAs.Add(aggregateA);
        dbContext.SaveChanges();

        var aggregateB = new AggregateB
        {
            RequiredText = "AggregateB",
            RequiredDateTimeOffset = DateTimeOffset.UtcNow,
        };
        
        dbContext.AggregateBs.Add(aggregateB);
        dbContext.SaveChanges();
    }

    public void Dispose()
    {
        ServiceProvider.Dispose();
    }
}