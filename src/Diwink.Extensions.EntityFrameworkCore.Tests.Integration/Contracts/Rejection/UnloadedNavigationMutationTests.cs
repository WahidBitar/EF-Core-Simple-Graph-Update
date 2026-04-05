using Diwink.Extensions.EntityFrameworkCore.Exceptions;
using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;
using Diwink.Extensions.EntityFrameworkCore.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Integration.Contracts.Rejection;

/// <summary>
/// Integration tests proving that mutations depending on unloaded navigations
/// are rejected (FR-015, FR-016). Only explicitly loaded navigations participate
/// in graph mutation.
/// </summary>
public class UnloadedNavigationMutationTests : IntegrationTestBase
{
    public UnloadedNavigationMutationTests(SqlServerContainerFixture fixture)
        : base(fixture) { }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await using var ctx = CreateContext();
        await SeedData.SeedFullScenarioAsync(ctx);
    }

    [Fact]
    public async Task Unloaded_collection_navigation_with_mutations_is_rejected()
    {
        // Arrange — load Course WITHOUT Tags navigation
        await using var ctx = CreateContext();
        var existing = await ctx.Courses
            .Include(c => c.Policy) // load policy but NOT tags
            .FirstAsync(c => c.Id == SeedData.Course1Id);

        // Updated graph tries to modify Tags (which wasn't loaded)
        var updated = new Course
        {
            Id = SeedData.Course1Id,
            CatalogId = SeedData.CatalogId,
            Title = existing.Title,
            Code = existing.Code,
            Policy = new CoursePolicy
            {
                CourseId = SeedData.Course1Id,
                PolicyVersion = existing.Policy!.PolicyVersion,
                IsMandatory = existing.Policy.IsMandatory
            },
            Tags =
            [
                new TopicTag { Id = Guid.NewGuid(), Label = "ShouldReject" }
            ]
        };

        // Act & Assert — should reject because Tags wasn't loaded
        var act = () => ctx.UpdateGraph(updated, existing);
        act.Should().Throw<UnloadedNavigationMutationException>()
            .Which.NavigationName.Should().Be("Tags");
    }

    [Fact]
    public async Task Unloaded_reference_navigation_with_mutations_is_rejected()
    {
        // Arrange — load Course WITHOUT Policy navigation
        await using var ctx = CreateContext();
        var existing = await ctx.Courses
            .Include(c => c.Tags) // load tags but NOT policy
            .FirstAsync(c => c.Id == SeedData.Course1Id);

        // Updated graph tries to modify Policy (which wasn't loaded)
        var updated = new Course
        {
            Id = SeedData.Course1Id,
            CatalogId = SeedData.CatalogId,
            Title = existing.Title,
            Code = existing.Code,
            Tags = existing.Tags.Select(t => new TopicTag
            {
                Id = t.Id,
                Label = t.Label
            }).ToList(),
            Policy = new CoursePolicy
            {
                CourseId = SeedData.Course1Id,
                PolicyVersion = "NewVersion",
                IsMandatory = false
            }
        };

        // Act & Assert — should reject because Policy wasn't loaded
        var act = () => ctx.UpdateGraph(updated, existing);
        act.Should().Throw<UnloadedNavigationMutationException>()
            .Which.NavigationName.Should().Be("Policy");
    }

    [Fact]
    public async Task Unloaded_navigation_with_no_mutations_is_silently_skipped()
    {
        // Arrange — load Course WITHOUT Tags navigation
        await using var ctx = CreateContext();
        var existing = await ctx.Courses
            .Include(c => c.Policy)
            .FirstAsync(c => c.Id == SeedData.Course1Id);

        // Updated graph does NOT provide Tags at all (empty/default)
        var updated = new Course
        {
            Id = SeedData.Course1Id,
            CatalogId = SeedData.CatalogId,
            Title = "Updated Title",
            Code = existing.Code,
            Policy = new CoursePolicy
            {
                CourseId = SeedData.Course1Id,
                PolicyVersion = existing.Policy!.PolicyVersion,
                IsMandatory = existing.Policy.IsMandatory
            }
        };

        // Act — should NOT throw because Tags is unloaded and not mutated
        ctx.UpdateGraph(updated, existing);
        await ctx.SaveChangesAsync();

        // Assert
        await using var verifyCtx = CreateContext();
        var result = await verifyCtx.Courses.FirstAsync(c => c.Id == SeedData.Course1Id);
        result.Title.Should().Be("Updated Title");
    }
}
