using Diwink.Extensions.EntityFrameworkCore.TestModel;
using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;
using Diwink.Extensions.EntityFrameworkCore.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Integration.Contracts.ManyToMany;

/// <summary>
/// Safety contract tests ensuring many-to-many unlink/remove operations never
/// accidentally delete related entities (FR-003).
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class ManyToManySafetyContractTests : IntegrationTestBase
{
    public ManyToManySafetyContractTests(SqlServerContainerFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task Remove_all_tag_associations_preserves_all_tag_entities()
    {
        // Arrange
        await using var seedCtx = CreateContext();
        await SeedData.SeedFullScenarioAsync(seedCtx);

        await using var ctx = CreateContext();
        var existing = await ctx.Courses
            .Include(c => c.Tags)
            .FirstAsync(c => c.Id == SeedData.Course1Id);

        // Remove all tags from course
        var updated = new Course
        {
            Id = SeedData.Course1Id,
            CatalogId = SeedData.CatalogId,
            Title = existing.Title,
            Code = existing.Code,
            Tags = []
        };

        // Act
        ctx.UpdateGraph(updated, existing);
        await ctx.SaveChangesAsync();

        // Assert
        await using var verifyCtx = CreateContext();
        var courseTags = await verifyCtx.Courses
            .Include(c => c.Tags)
            .FirstAsync(c => c.Id == SeedData.Course1Id);
        courseTags.Tags.Should().BeEmpty();

        // All tag entities must still exist
        var tagCount = await verifyCtx.TopicTags.CountAsync();
        tagCount.Should().Be(3, "removing all many-to-many links must not delete any tag entities");
    }

    [Fact]
    public async Task Remove_all_assignments_preserves_all_mentor_entities()
    {
        // Arrange
        await using var seedCtx = CreateContext();
        await SeedData.SeedFullScenarioAsync(seedCtx);

        await using var ctx = CreateContext();
        var existing = await ctx.Courses
            .Include(c => c.MentorAssignments)
            .FirstAsync(c => c.Id == SeedData.Course1Id);

        var updated = new Course
        {
            Id = SeedData.Course1Id,
            CatalogId = SeedData.CatalogId,
            Title = existing.Title,
            Code = existing.Code,
            MentorAssignments = []
        };

        // Act
        ctx.UpdateGraph(updated, existing);
        await ctx.SaveChangesAsync();

        // Assert
        await using var verifyCtx = CreateContext();
        var mentorCount = await verifyCtx.Mentors.CountAsync();
        mentorCount.Should().Be(2, "removing all payload associations must not delete any mentor entities");
    }
}
