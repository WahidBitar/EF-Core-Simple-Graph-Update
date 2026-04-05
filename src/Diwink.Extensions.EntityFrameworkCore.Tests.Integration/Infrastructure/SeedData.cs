using Diwink.Extensions.EntityFrameworkCore.TestModel;
using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Integration.Infrastructure;

/// <summary>
/// Deterministic seed data used across integration contract tests.
/// Exposes well-known IDs for assertions and seeds complete test scenarios.
/// </summary>
public static class SeedData
{
    // Well-known IDs for deterministic test scenarios
    public static readonly Guid CatalogId = Guid.Parse("a1000000-0000-0000-0000-000000000001");
    public static readonly Guid Course1Id = Guid.Parse("b1000000-0000-0000-0000-000000000001");
    public static readonly Guid Course2Id = Guid.Parse("b1000000-0000-0000-0000-000000000002");
    public static readonly Guid Tag1Id = Guid.Parse("c1000000-0000-0000-0000-000000000001");
    public static readonly Guid Tag2Id = Guid.Parse("c1000000-0000-0000-0000-000000000002");
    public static readonly Guid Tag3Id = Guid.Parse("c1000000-0000-0000-0000-000000000003");
    public static readonly Guid Mentor1Id = Guid.Parse("d1000000-0000-0000-0000-000000000001");
    public static readonly Guid Mentor2Id = Guid.Parse("d1000000-0000-0000-0000-000000000002");
    public static readonly Guid Workspace1Id = Guid.Parse("e1000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Seeds a complete test scenario with all relationship types represented.
    /// <summary>
    /// Seeds the provided test database with a complete, deterministic integration scenario: three topic tags, two mentors (one with a workspace), a learning catalog containing two courses with policies, tag associations, and a mentor assignment for the first course, then saves the changes to the context.
    /// </summary>
    /// <param name="context">The TestDbContext to which seeded entities are added and persisted.</param>
    public static async Task SeedFullScenarioAsync(TestDbContext context)
    {
        var tag1 = new TopicTag { Id = Tag1Id, Label = "Architecture" };
        var tag2 = new TopicTag { Id = Tag2Id, Label = "Testing" };
        var tag3 = new TopicTag { Id = Tag3Id, Label = "Security" };

        var mentor1 = new Mentor
        {
            Id = Mentor1Id,
            DisplayName = "Alice",
            Status = "Active",
            Workspace = new MentorWorkspace
            {
                Id = Workspace1Id,
                MentorId = Mentor1Id,
                DeskCode = "D-101",
                Building = "HQ"
            }
        };

        var mentor2 = new Mentor
        {
            Id = Mentor2Id,
            DisplayName = "Bob",
            Status = "Active"
        };

        var catalog = new LearningCatalog
        {
            Id = CatalogId,
            Name = "Engineering Fundamentals",
            Courses =
            [
                new Course
                {
                    Id = Course1Id,
                    CatalogId = CatalogId,
                    Title = "Software Design",
                    Code = "SD-101",
                    Policy = new CoursePolicy
                    {
                        CourseId = Course1Id,
                        PolicyVersion = "1.0",
                        IsMandatory = true
                    },
                    Tags = [tag1, tag2],
                    MentorAssignments =
                    [
                        new CourseMentorAssignment
                        {
                            CourseId = Course1Id,
                            MentorId = Mentor1Id,
                            Role = "Lead",
                            AssignedOnUtc = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                            AllocationPercent = 75m
                        }
                    ]
                },
                new Course
                {
                    Id = Course2Id,
                    CatalogId = CatalogId,
                    Title = "Security Patterns",
                    Code = "SP-201",
                    Policy = new CoursePolicy
                    {
                        CourseId = Course2Id,
                        PolicyVersion = "2.0",
                        IsMandatory = false
                    },
                    Tags = [tag2, tag3]
                }
            ]
        };

        context.Mentors.AddRange(mentor1, mentor2);
        context.LearningCatalogs.Add(catalog);
        await context.SaveChangesAsync();
    }
}
