using Diwink.Extensions.EntityFrameworkCore.TestModel;
using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Unit.GraphDiff;

public class DbContextExtensionsTests
{
    private static TestDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public void InsertUpdateOrDeleteGraph_throws_when_context_is_null()
    {
        var updated = new Course { Id = Guid.NewGuid(), CatalogId = Guid.NewGuid(), Title = "Updated", Code = "UPD-1" };
        var existing = new Course { Id = updated.Id, CatalogId = updated.CatalogId, Title = "Existing", Code = "EX-1" };

        var act = () => DbContextExtensions.UpdateGraph<Course>(null!, updated, existing);

        act.Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("context");
    }

    [Fact]
    public void InsertUpdateOrDeleteGraph_throws_when_updated_entity_is_null()
    {
        using var context = CreateInMemoryContext();
        var existing = new Course { Id = Guid.NewGuid(), CatalogId = Guid.NewGuid(), Title = "Existing", Code = "EX-1" };

        var act = () => context.UpdateGraph<Course>(null!, existing);

        act.Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("updatedEntity");
    }

    [Fact]
    public void InsertUpdateOrDeleteGraph_throws_when_existing_entity_is_null()
    {
        using var context = CreateInMemoryContext();
        var updated = new Course { Id = Guid.NewGuid(), CatalogId = Guid.NewGuid(), Title = "Updated", Code = "UPD-1" };

        var act = () => context.UpdateGraph(updated, null!);

        act.Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("existingEntity");
    }
}
