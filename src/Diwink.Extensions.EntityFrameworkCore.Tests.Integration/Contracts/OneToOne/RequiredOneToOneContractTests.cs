using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;
using Diwink.Extensions.EntityFrameworkCore.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Integration.Contracts.OneToOne;

/// <summary>
/// Integration contract tests for required one-to-one (Course -> CoursePolicy).
/// Required dependent removal => delete (FR-007, FR-008).
/// </summary>
public class RequiredOneToOneContractTests : IntegrationTestBase
{
    public RequiredOneToOneContractTests(SqlServerContainerFixture fixture)
        : base(fixture) { }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await using var ctx = CreateContext();
        await SeedData.SeedFullScenarioAsync(ctx);
    }

    [Fact]
    public async Task Remove_required_dependent_deletes_it()
    {
        // Arrange — load Course1 with its required Policy
        await using var ctx = CreateContext();
        var existing = await ctx.Courses
            .Include(c => c.Policy)
            .FirstAsync(c => c.Id == SeedData.Course1Id);

        existing.Policy.Should().NotBeNull("seed data includes a CoursePolicy");

        // Updated graph removes the Policy reference
        var updated = new Course
        {
            Id = SeedData.Course1Id,
            CatalogId = SeedData.CatalogId,
            Title = existing.Title,
            Code = existing.Code,
            Policy = null
        };

        // Act
        ctx.UpdateGraph(updated, existing);
        await ctx.SaveChangesAsync();

        // Assert — the CoursePolicy row should be deleted
        await using var verifyCtx = CreateContext();
        var course = await verifyCtx.Courses
            .Include(c => c.Policy)
            .FirstAsync(c => c.Id == SeedData.Course1Id);
        course.Policy.Should().BeNull();

        var policyExists = await verifyCtx.Set<CoursePolicy>()
            .AnyAsync(p => p.CourseId == SeedData.Course1Id);
        policyExists.Should().BeFalse("required dependent should be deleted, not just detached");
    }

    [Fact]
    public async Task Update_required_dependent_scalar_properties()
    {
        // Arrange
        await using var ctx = CreateContext();
        var existing = await ctx.Courses
            .Include(c => c.Policy)
            .FirstAsync(c => c.Id == SeedData.Course1Id);

        var updated = new Course
        {
            Id = SeedData.Course1Id,
            CatalogId = SeedData.CatalogId,
            Title = existing.Title,
            Code = existing.Code,
            Policy = new CoursePolicy
            {
                CourseId = SeedData.Course1Id,
                PolicyVersion = "2.0-updated",
                IsMandatory = false
            }
        };

        // Act
        ctx.UpdateGraph(updated, existing);
        await ctx.SaveChangesAsync();

        // Assert
        await using var verifyCtx = CreateContext();
        var policy = await verifyCtx.Set<CoursePolicy>()
            .FirstAsync(p => p.CourseId == SeedData.Course1Id);
        policy.PolicyVersion.Should().Be("2.0-updated");
        policy.IsMandatory.Should().BeFalse();
    }

    [Fact]
    public async Task Add_required_dependent_when_none_exists()
    {
        // Arrange — remove existing policy first
        await using (var setupCtx = CreateContext())
        {
            var policy = await setupCtx.Set<CoursePolicy>()
                .FirstOrDefaultAsync(p => p.CourseId == SeedData.Course2Id);
            if (policy is not null)
            {
                setupCtx.Remove(policy);
                await setupCtx.SaveChangesAsync();
            }
        }

        await using var ctx = CreateContext();
        var existing = await ctx.Courses
            .Include(c => c.Policy)
            .FirstAsync(c => c.Id == SeedData.Course2Id);

        existing.Policy.Should().BeNull("we removed it in setup");

        var updated = new Course
        {
            Id = SeedData.Course2Id,
            CatalogId = SeedData.CatalogId,
            Title = existing.Title,
            Code = existing.Code,
            Policy = new CoursePolicy
            {
                CourseId = SeedData.Course2Id,
                PolicyVersion = "3.0-new",
                IsMandatory = true
            }
        };

        // Act
        ctx.UpdateGraph(updated, existing);
        await ctx.SaveChangesAsync();

        // Assert
        await using var verifyCtx = CreateContext();
        var result = await verifyCtx.Courses
            .Include(c => c.Policy)
            .FirstAsync(c => c.Id == SeedData.Course2Id);
        result.Policy.Should().NotBeNull();
        result.Policy!.PolicyVersion.Should().Be("3.0-new");
    }
}
