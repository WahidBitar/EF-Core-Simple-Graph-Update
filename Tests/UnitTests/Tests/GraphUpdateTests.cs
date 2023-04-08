namespace UnitTests.Tests;

[TestCaseOrderer(ordererTypeName: "UnitTests.PriorityOrderer", ordererAssemblyName: "UnitTests")]
public class GraphUpdateTests : IClassFixture<TestDbFixture>, IDisposable
{
    private readonly TestModelDbContext _dbContext;
    private readonly IServiceScope _scope;

    public GraphUpdateTests(TestDbFixture testDbFixture)
    {
        _scope = testDbFixture.ServiceProvider.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<TestModelDbContext>();
    }

    [Fact]
    [TestPriority(1)]
    public async Task Set_AggregateId()
    {
        // we're going to simulate the update of the mapped model.
        // Consider that you have kinda DTO and you've mapped it to the following instance.
        var mappedModel = new AggregateB
        {
            Id = 1,
            RequiredText = "AggregateB",
            RequiredDateTimeOffset = DateTimeOffset.UtcNow,
            AggregateAId = 1,
        };
        var dbModel = await _dbContext.AggregateBs.FirstOrDefaultAsync();

        _dbContext.UpdateGraph(mappedModel, dbModel);
        await _dbContext.SaveChangesAsync();

        dbModel = await _dbContext
            .AggregateBs
            .Include(x => x.RelatedAggregate)
            .FirstOrDefaultAsync();

        Assert.NotNull(dbModel?.RelatedAggregate);
    }

    [Fact]
    [TestPriority(2)]
    public async Task Add_An_Object_To_The_Navigation()
    {
        var mappedModel = new AggregateA
        {
            Id = 1,
            RequiredText = "AggregateA Updated",
            RelatedAggregates = new List<AggregateB>
            {
                new()
                {
                    Id = 1,
                    RequiredText = "AggregateB 1",
                    RequiredDateTimeOffset = DateTimeOffset.UtcNow,
                    AggregateAId = 1,
                },
                new()
                {
                    RequiredText = "AggregateB 2",
                    RequiredDateTimeOffset = DateTimeOffset.UtcNow,
                },
            }
        };

        var dbModel = await _dbContext
            .AggregateAs
            .Include(x => x.RelatedAggregates)
            .FirstOrDefaultAsync();

        _dbContext.UpdateGraph(mappedModel, dbModel);
        await _dbContext.SaveChangesAsync();

        dbModel = await _dbContext
            .AggregateAs
            .Include(x => x.RelatedAggregates)
            .FirstAsync();

        Assert.Equal(2, dbModel.RelatedAggregates.Count);
    }

    [Fact]
    [TestPriority(2)]
    public async Task Full_Update_Composite_Navigation()
    {
        var mappedModel = new AggregateA
        {
            Id = 1,
            RequiredText = "AggregateA Updated",
            CompositeEntities = new List<CompositeEntity>
            {
                new()
                {
                    RequiredText = "AggregateA Comp1 Updated",
                    SomeId = 1,
                    AggregateAId = 1,
                },
                new()
                {
                    RequiredText = "AggregateA Comp3",
                    SomeId = 3,
                    AggregateAId = 1,
                }
            }
        };
        
        var dbModel = await _dbContext
            .AggregateAs
            .Include(x => x.CompositeEntities)
            .FirstOrDefaultAsync();

        _dbContext.UpdateGraph(mappedModel, dbModel);
        await _dbContext.SaveChangesAsync();

        dbModel = await _dbContext
            .AggregateAs
            .Include(x => x.CompositeEntities)
            .FirstAsync();

        Assert.Equal(2, dbModel.CompositeEntities.Count);
        Assert.True(dbModel.CompositeEntities.All(e => e.AggregateAId == 1));
        Assert.Contains(dbModel.CompositeEntities, e => e.SomeId == 1 && e.RequiredText.Contains("Updated"));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _scope.Dispose();
    }
}