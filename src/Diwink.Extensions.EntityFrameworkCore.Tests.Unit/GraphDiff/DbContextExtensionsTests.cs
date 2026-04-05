using Diwink.Extensions.EntityFrameworkCore.TestModel;
using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Unit.GraphDiff;

public class DbContextExtensionsTests
{
    private static TestDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public void InsertUpdateOrDeleteGraph_throws_when_context_is_null()
    {
        var updated = new Course { Id = Guid.NewGuid(), CatalogId = Guid.NewGuid(), Title = "Updated", Code = "UPD-1" };
        var existing = new Course { Id = updated.Id, CatalogId = updated.CatalogId, Title = "Existing", Code = "EX-1" };

        var act = () => DbContextExtensions.InsertUpdateOrDeleteGraph<Course>(null!, updated, existing);

        act.Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("context");
    }

    [Fact]
    public void InsertUpdateOrDeleteGraph_throws_when_updated_entity_is_null()
    {
        using var context = CreateInMemoryContext();
        var existing = new Course { Id = Guid.NewGuid(), CatalogId = Guid.NewGuid(), Title = "Existing", Code = "EX-1" };

        var act = () => context.InsertUpdateOrDeleteGraph<Course>(null!, existing);

        act.Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("updatedEntity");
    }

    [Fact]
    public void InsertUpdateOrDeleteGraph_throws_when_existing_entity_is_null()
    {
        using var context = CreateInMemoryContext();
        var updated = new Course { Id = Guid.NewGuid(), CatalogId = Guid.NewGuid(), Title = "Updated", Code = "UPD-1" };

        var act = () => context.InsertUpdateOrDeleteGraph(updated, null!);

        act.Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("existingEntity");
    }

    [Fact]
    public async Task InsertUpdateOrDeleteGraph_updates_scalar_properties_on_root_entity()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var courseId = Guid.NewGuid();
        var catalogId = Guid.NewGuid();

        {
            await using var seedCtx = CreateInMemoryContext(dbName);
            seedCtx.LearningCatalogs.Add(new LearningCatalog { Id = catalogId, Name = "Cat" });
            seedCtx.Courses.Add(new Course
            {
                Id = courseId,
                CatalogId = catalogId,
                Title = "Original Title",
                Code = "ORIG-001"
            });
            await seedCtx.SaveChangesAsync();
        }

        {
            await using var ctx = CreateInMemoryContext(dbName);
            var existing = await ctx.Courses.FirstAsync(c => c.Id == courseId);

            var updated = new Course
            {
                Id = courseId,
                CatalogId = catalogId,
                Title = "Updated Title",
                Code = "UPDT-001"
            };

            ctx.InsertUpdateOrDeleteGraph(updated, existing);
            await ctx.SaveChangesAsync();
        }

        {
            await using var verifyCtx = CreateInMemoryContext(dbName);
            var course = await verifyCtx.Courses.FirstAsync(c => c.Id == courseId);
            course.Title.Should().Be("Updated Title");
            course.Code.Should().Be("UPDT-001");
        }
    }

    [Fact]
    public async Task InsertUpdateOrDeleteGraph_returns_the_tracked_entity()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var courseId = Guid.NewGuid();
        var catalogId = Guid.NewGuid();

        {
            await using var seedCtx = CreateInMemoryContext(dbName);
            seedCtx.LearningCatalogs.Add(new LearningCatalog { Id = catalogId, Name = "Cat" });
            seedCtx.Courses.Add(new Course
            {
                Id = courseId,
                CatalogId = catalogId,
                Title = "Original",
                Code = "C-001"
            });
            await seedCtx.SaveChangesAsync();
        }

        await using var ctx = CreateInMemoryContext(dbName);
        var existing = await ctx.Courses.FirstAsync(c => c.Id == courseId);

        var updated = new Course
        {
            Id = courseId,
            CatalogId = catalogId,
            Title = "Changed",
            Code = "C-001"
        };

        var result = ctx.InsertUpdateOrDeleteGraph(updated, existing);

        result.Should().NotBeNull();
        result.Should().BeSameAs(existing, "the method should return the tracked (existing) entity, not the detached one");
    }
}