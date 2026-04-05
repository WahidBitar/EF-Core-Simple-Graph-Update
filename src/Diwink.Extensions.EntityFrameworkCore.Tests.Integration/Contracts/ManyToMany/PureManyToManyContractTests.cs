using Diwink.Extensions.EntityFrameworkCore.TestModel;
using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;
using Diwink.Extensions.EntityFrameworkCore.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Integration.Contracts.ManyToMany;

/// <summary>
/// Contract tests for pure many-to-many (skip navigation) add/update/unlink outcomes.
/// Validates FR-002, FR-003, FR-004 for pure association membership.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class PureManyToManyContractTests : IntegrationTestBase
{
    public PureManyToManyContractTests(SqlServerContainerFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task Add_new_tag_association_creates_link_without_affecting_existing_tags()
    {
        // Arrange
        await using var seedCtx = CreateContext();
        await SeedData.SeedFullScenarioAsync(seedCtx);

        await using var ctx = CreateContext();
        var existing = await ctx.Courses
            .Include(c => c.Tags)
            .FirstAsync(c => c.Id == SeedData.Course1Id);

        var updated = new Course
        {
            Id = SeedData.Course1Id,
            CatalogId = SeedData.CatalogId,
            Title = existing.Title,
            Code = existing.Code,
            Tags =
            [
                new TopicTag { Id = SeedData.Tag1Id, Label = "Architecture" },
                new TopicTag { Id = SeedData.Tag2Id, Label = "Testing" },
                new TopicTag { Id = SeedData.Tag3Id, Label = "Security" } // new link
            ]
        };

        // Act
        ctx.UpdateGraph(updated, existing);
        await ctx.SaveChangesAsync();

        // Assert
        await using var verifyCtx = CreateContext();
        var result = await verifyCtx.Courses
            .Include(c => c.Tags)
            .FirstAsync(c => c.Id == SeedData.Course1Id);

        result.Tags.Should().HaveCount(3);
        result.Tags.Select(t => t.Id).Should().Contain(SeedData.Tag3Id);
    }

    [Fact]
    public async Task Remove_tag_association_unlinks_without_deleting_the_tag_entity()
    {
        // Arrange
        await using var seedCtx = CreateContext();
        await SeedData.SeedFullScenarioAsync(seedCtx);

        await using var ctx = CreateContext();
        var existing = await ctx.Courses
            .Include(c => c.Tags)
            .FirstAsync(c => c.Id == SeedData.Course1Id);

        // Remove Tag2 ("Testing") from course, keep Tag1 only
        var updated = new Course
        {
            Id = SeedData.Course1Id,
            CatalogId = SeedData.CatalogId,
            Title = existing.Title,
            Code = existing.Code,
            Tags =
            [
                new TopicTag { Id = SeedData.Tag1Id, Label = "Architecture" }
            ]
        };

        // Act
        ctx.UpdateGraph(updated, existing);
        await ctx.SaveChangesAsync();

        // Assert
        await using var verifyCtx = CreateContext();
        var result = await verifyCtx.Courses
            .Include(c => c.Tags)
            .FirstAsync(c => c.Id == SeedData.Course1Id);

        result.Tags.Should().HaveCount(1);
        result.Tags.Single().Id.Should().Be(SeedData.Tag1Id);

        // Tag2 entity must still exist in the database (FR-003: unlink, not delete)
        var tag2Exists = await verifyCtx.TopicTags.AnyAsync(t => t.Id == SeedData.Tag2Id);
        tag2Exists.Should().BeTrue("removing a many-to-many link must not delete the related entity");
    }

    [Fact]
    public async Task Add_link_to_new_related_entity_creates_both_entity_and_link()
    {
        // Arrange
        await using var seedCtx = CreateContext();
        await SeedData.SeedFullScenarioAsync(seedCtx);

        await using var ctx = CreateContext();
        var existing = await ctx.Courses
            .Include(c => c.Tags)
            .FirstAsync(c => c.Id == SeedData.Course1Id);

        var newTagId = Guid.Parse("c1000000-0000-0000-0000-000000000099");
        var updated = new Course
        {
            Id = SeedData.Course1Id,
            CatalogId = SeedData.CatalogId,
            Title = existing.Title,
            Code = existing.Code,
            Tags =
            [
                new TopicTag { Id = SeedData.Tag1Id, Label = "Architecture" },
                new TopicTag { Id = SeedData.Tag2Id, Label = "Testing" },
                new TopicTag { Id = newTagId, Label = "Observability" } // brand new entity
            ]
        };

        // Act
        ctx.UpdateGraph(updated, existing);
        await ctx.SaveChangesAsync();

        // Assert
        await using var verifyCtx = CreateContext();
        var result = await verifyCtx.Courses
            .Include(c => c.Tags)
            .FirstAsync(c => c.Id == SeedData.Course1Id);

        result.Tags.Should().HaveCount(3);
        var newTag = await verifyCtx.TopicTags.FindAsync(newTagId);
        newTag.Should().NotBeNull();
        newTag!.Label.Should().Be("Observability");
    }

    [Fact]
    public async Task Update_existing_related_entity_through_association_updates_entity_state()
    {
        // Arrange
        await using var seedCtx = CreateContext();
        await SeedData.SeedFullScenarioAsync(seedCtx);

        await using var ctx = CreateContext();
        var existing = await ctx.Courses
            .Include(c => c.Tags)
            .FirstAsync(c => c.Id == SeedData.Course1Id);

        var updated = new Course
        {
            Id = SeedData.Course1Id,
            CatalogId = SeedData.CatalogId,
            Title = existing.Title,
            Code = existing.Code,
            Tags =
            [
                new TopicTag { Id = SeedData.Tag1Id, Label = "Software Architecture" }, // updated label
                new TopicTag { Id = SeedData.Tag2Id, Label = "Testing" }
            ]
        };

        // Act
        ctx.UpdateGraph(updated, existing);
        await ctx.SaveChangesAsync();

        // Assert
        await using var verifyCtx = CreateContext();
        var tag1 = await verifyCtx.TopicTags.FindAsync(SeedData.Tag1Id);
        tag1!.Label.Should().Be("Software Architecture");
    }
}
