using Diwink.Extensions.EntityFrameworkCore.TestModel;
using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Integration.Infrastructure;

/// <summary>
/// Unit-style tests (no container required) for the SeedData helper.
/// Validates that well-known IDs are distinct, non-empty, and that the
/// seed method produces the expected entity graph using an in-memory store.
/// </summary>
public class SeedDataStructureTests
{
    // -------------------------------------------------------------------------
    // Well-known ID distinctness
    // -------------------------------------------------------------------------

    [Fact]
    public void All_well_known_ids_are_distinct()
    {
        var ids = new[]
        {
            SeedData.CatalogId,
            SeedData.Course1Id,
            SeedData.Course2Id,
            SeedData.Tag1Id,
            SeedData.Tag2Id,
            SeedData.Tag3Id,
            SeedData.Mentor1Id,
            SeedData.Mentor2Id,
            SeedData.Workspace1Id
        };

        ids.Should().OnlyHaveUniqueItems("each well-known ID must be unique to prevent test cross-contamination");
    }

    [Fact]
    public void All_well_known_ids_are_non_empty_guids()
    {
        SeedData.CatalogId.Should().NotBeEmpty();
        SeedData.Course1Id.Should().NotBeEmpty();
        SeedData.Course2Id.Should().NotBeEmpty();
        SeedData.Tag1Id.Should().NotBeEmpty();
        SeedData.Tag2Id.Should().NotBeEmpty();
        SeedData.Tag3Id.Should().NotBeEmpty();
        SeedData.Mentor1Id.Should().NotBeEmpty();
        SeedData.Mentor2Id.Should().NotBeEmpty();
        SeedData.Workspace1Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Course1Id_and_Course2Id_share_same_catalog_prefix()
    {
        // Both courses belong to the same catalog — verify IDs encode distinct entities
        SeedData.Course1Id.Should().NotBe(SeedData.Course2Id);
    }

    // -------------------------------------------------------------------------
    // SeedFullScenarioAsync — entity graph shape
    // -------------------------------------------------------------------------

    private static TestDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(options);
    }

    [Fact]
    public async Task SeedFullScenarioAsync_creates_correct_number_of_entities()
    {
        await using var ctx = CreateInMemoryContext();
        await SeedData.SeedFullScenarioAsync(ctx);

        (await ctx.LearningCatalogs.CountAsync()).Should().Be(1);
        (await ctx.Courses.CountAsync()).Should().Be(2);
        (await ctx.TopicTags.CountAsync()).Should().Be(3);
        (await ctx.Mentors.CountAsync()).Should().Be(2);
        (await ctx.MentorWorkspaces.CountAsync()).Should().Be(1);
        (await ctx.CoursePolicies.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task SeedFullScenarioAsync_mentor1_has_workspace_mentor2_does_not()
    {
        await using var ctx = CreateInMemoryContext();
        await SeedData.SeedFullScenarioAsync(ctx);

        var mentor1 = await ctx.Mentors
            .Include(m => m.Workspace)
            .FirstAsync(m => m.Id == SeedData.Mentor1Id);

        var mentor2 = await ctx.Mentors
            .Include(m => m.Workspace)
            .FirstAsync(m => m.Id == SeedData.Mentor2Id);

        mentor1.Workspace.Should().NotBeNull("seed data assigns Workspace1 to Mentor1");
        mentor1.Workspace!.Id.Should().Be(SeedData.Workspace1Id);
        mentor2.Workspace.Should().BeNull("seed data has no workspace for Mentor2");
    }

    [Fact]
    public async Task SeedFullScenarioAsync_course1_has_two_tags_architecture_and_testing()
    {
        await using var ctx = CreateInMemoryContext();
        await SeedData.SeedFullScenarioAsync(ctx);

        var course1 = await ctx.Courses
            .Include(c => c.Tags)
            .FirstAsync(c => c.Id == SeedData.Course1Id);

        course1.Tags.Should().HaveCount(2);
        course1.Tags.Select(t => t.Id).Should().Contain(SeedData.Tag1Id);
        course1.Tags.Select(t => t.Id).Should().Contain(SeedData.Tag2Id);
    }

    [Fact]
    public async Task SeedFullScenarioAsync_course2_has_two_tags_testing_and_security()
    {
        await using var ctx = CreateInMemoryContext();
        await SeedData.SeedFullScenarioAsync(ctx);

        var course2 = await ctx.Courses
            .Include(c => c.Tags)
            .FirstAsync(c => c.Id == SeedData.Course2Id);

        course2.Tags.Should().HaveCount(2);
        course2.Tags.Select(t => t.Id).Should().Contain(SeedData.Tag2Id);
        course2.Tags.Select(t => t.Id).Should().Contain(SeedData.Tag3Id);
    }

    [Fact]
    public async Task SeedFullScenarioAsync_course1_has_one_mentor_assignment_for_mentor1()
    {
        await using var ctx = CreateInMemoryContext();
        await SeedData.SeedFullScenarioAsync(ctx);

        var assignments = await ctx.CourseMentorAssignments
            .Where(a => a.CourseId == SeedData.Course1Id)
            .ToListAsync();

        assignments.Should().HaveCount(1);
        assignments[0].MentorId.Should().Be(SeedData.Mentor1Id);
        assignments[0].Role.Should().Be("Lead");
        assignments[0].AllocationPercent.Should().Be(75m);
    }

    [Fact]
    public async Task SeedFullScenarioAsync_course2_has_no_mentor_assignments()
    {
        await using var ctx = CreateInMemoryContext();
        await SeedData.SeedFullScenarioAsync(ctx);

        var assignments = await ctx.CourseMentorAssignments
            .Where(a => a.CourseId == SeedData.Course2Id)
            .ToListAsync();

        assignments.Should().BeEmpty("seed data only assigns Mentor1 to Course1");
    }

    [Fact]
    public async Task SeedFullScenarioAsync_course1_policy_is_mandatory_version_1_0()
    {
        await using var ctx = CreateInMemoryContext();
        await SeedData.SeedFullScenarioAsync(ctx);

        var policy = await ctx.CoursePolicies
            .FirstAsync(p => p.CourseId == SeedData.Course1Id);

        policy.PolicyVersion.Should().Be("1.0");
        policy.IsMandatory.Should().BeTrue();
    }

    [Fact]
    public async Task SeedFullScenarioAsync_course2_policy_is_optional_version_2_0()
    {
        await using var ctx = CreateInMemoryContext();
        await SeedData.SeedFullScenarioAsync(ctx);

        var policy = await ctx.CoursePolicies
            .FirstAsync(p => p.CourseId == SeedData.Course2Id);

        policy.PolicyVersion.Should().Be("2.0");
        policy.IsMandatory.Should().BeFalse();
    }

    [Fact]
    public async Task SeedFullScenarioAsync_both_courses_belong_to_the_single_catalog()
    {
        await using var ctx = CreateInMemoryContext();
        await SeedData.SeedFullScenarioAsync(ctx);

        var courses = await ctx.Courses.ToListAsync();
        courses.Should().AllSatisfy(c => c.CatalogId.Should().Be(SeedData.CatalogId));
    }

    [Fact]
    public async Task SeedFullScenarioAsync_tag2_is_shared_across_both_courses()
    {
        // Tag2 ("Testing") is linked to both Course1 and Course2 — validates M:M join rows
        await using var ctx = CreateInMemoryContext();
        await SeedData.SeedFullScenarioAsync(ctx);

        var course1 = await ctx.Courses.Include(c => c.Tags).FirstAsync(c => c.Id == SeedData.Course1Id);
        var course2 = await ctx.Courses.Include(c => c.Tags).FirstAsync(c => c.Id == SeedData.Course2Id);

        course1.Tags.Select(t => t.Id).Should().Contain(SeedData.Tag2Id);
        course2.Tags.Select(t => t.Id).Should().Contain(SeedData.Tag2Id);
    }
}