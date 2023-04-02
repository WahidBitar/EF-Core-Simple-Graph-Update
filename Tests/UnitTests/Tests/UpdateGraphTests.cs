
namespace UnitTests.Tests;

[TestCaseOrderer(ordererTypeName: "UnitTests.PriorityOrderer", ordererAssemblyName: "UnitTests")]
public class UpdateGraphTests : IDisposable
{
    private bool inMemoryDb;
    private readonly ServiceProvider serviceProvider;
    private IServiceScope scope;
    private TestModelDbContext dbContext;
    
    public UpdateGraphTests()
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

        serviceProvider = services.BuildServiceProvider();


        scope = serviceProvider.CreateScope();
        dbContext = scope.ServiceProvider.GetRequiredService<TestModelDbContext>();
    }

    public void Dispose()
    {
        scope.Dispose();
        //dbContext.Dispose();
        serviceProvider.Dispose();
    }
}