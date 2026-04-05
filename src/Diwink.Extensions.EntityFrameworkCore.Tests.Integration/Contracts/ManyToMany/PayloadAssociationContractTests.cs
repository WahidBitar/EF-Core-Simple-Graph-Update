using Diwink.Extensions.EntityFrameworkCore.TestModel;
using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;
using Diwink.Extensions.EntityFrameworkCore.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Integration.Contracts.ManyToMany;

/// <summary>
/// Contract tests for many-to-many with payload (association entity) create/update/remove.
/// Validates FR-002, FR-004, FR-005, FR-006 for payload associations.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class PayloadAssociationContractTests : IntegrationTestBase
{
    public PayloadAssociationContractTests(SqlServerContainerFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task Add_new_assignment_creates_association_entity_with_payload()
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
            MentorAssignments =
            [
                new CourseMentorAssignment
                {
                    CourseId = SeedData.Course1Id,
                    MentorId = SeedData.Mentor1Id,
                    Role = "Lead",
                    AssignedOnUtc = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                    AllocationPercent = 75m
                },
                new CourseMentorAssignment
                {
                    CourseId = SeedData.Course1Id,
                    MentorId = SeedData.Mentor2Id,
                    Role = "Reviewer",
                    AssignedOnUtc = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                    AllocationPercent = 25m
                }
            ]
        };

        // Act
        ctx.UpdateGraph(updated, existing);
        await ctx.SaveChangesAsync();

        // Assert
        await using var verifyCtx = CreateContext();
        var assignments = await verifyCtx.CourseMentorAssignments
            .Where(a => a.CourseId == SeedData.Course1Id)
            .ToListAsync();

        assignments.Should().HaveCount(2);
        var newAssignment = assignments.Single(a => a.MentorId == SeedData.Mentor2Id);
        newAssignment.Role.Should().Be("Reviewer");
        newAssignment.AllocationPercent.Should().Be(25m);
    }

    [Fact]
    public async Task Update_assignment_payload_preserves_link_and_updates_fields()
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
            MentorAssignments =
            [
                new CourseMentorAssignment
                {
                    CourseId = SeedData.Course1Id,
                    MentorId = SeedData.Mentor1Id,
                    Role = "Senior Lead", // updated payload
                    AssignedOnUtc = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                    AllocationPercent = 100m // updated payload
                }
            ]
        };

        // Act
        ctx.UpdateGraph(updated, existing);
        await ctx.SaveChangesAsync();

        // Assert
        await using var verifyCtx = CreateContext();
        var assignment = await verifyCtx.CourseMentorAssignments
            .SingleAsync(a => a.CourseId == SeedData.Course1Id && a.MentorId == SeedData.Mentor1Id);

        assignment.Role.Should().Be("Senior Lead");
        assignment.AllocationPercent.Should().Be(100m);
    }

    [Fact]
    public async Task Remove_assignment_removes_association_entity_preserves_mentor()
    {
        // Arrange
        await using var seedCtx = CreateContext();
        await SeedData.SeedFullScenarioAsync(seedCtx);

        await using var ctx = CreateContext();
        var existing = await ctx.Courses
            .Include(c => c.MentorAssignments)
            .FirstAsync(c => c.Id == SeedData.Course1Id);

        // Remove Mentor1's assignment — empty assignments list
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
        var assignments = await verifyCtx.CourseMentorAssignments
            .Where(a => a.CourseId == SeedData.Course1Id)
            .ToListAsync();
        assignments.Should().BeEmpty();

        // Mentor1 must still exist (FR-003 equivalent for payload associations)
        var mentorExists = await verifyCtx.Mentors.AnyAsync(m => m.Id == SeedData.Mentor1Id);
        mentorExists.Should().BeTrue("removing an association entity must not delete the related mentor");
    }
}
