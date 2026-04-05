using Diwink.Extensions.EntityFrameworkCore.TestModel;
using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Unit.RelationshipSemantics;

/// <summary>
/// Unit tests for many-to-many diff strategy logic.
/// Uses InMemory provider for fast isolated testing of diff mechanics.
/// </summary>
public class ManyToManyDiffStrategyTests
{
    private static TestDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(options);
    }

    [Fact]
    public async Task Detects_added_skip_navigation_entries()
    {
        // Arrange — use separate contexts to avoid InMemory tracker conflicts
        var dbName = Guid.NewGuid().ToString();
        var catalogId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var tag1Id = Guid.NewGuid();
        var tag2Id = Guid.NewGuid();

        // Seed
        {
            await using var seedCtx = CreateInMemoryContext(dbName);
            var catalog = new LearningCatalog { Id = catalogId, Name = "Cat" };
            var tag1 = new TopicTag { Id = tag1Id, Label = "Existing" };
            var course = new Course
            {
                Id = courseId,
                CatalogId = catalogId,
                Title = "Test",
                Code = "T-001",
                Tags = [tag1]
            };
            seedCtx.LearningCatalogs.Add(catalog);
            seedCtx.Courses.Add(course);
            await seedCtx.SaveChangesAsync();
        }

        // Act — load, diff, save in a fresh context
        {
            await using var ctx = CreateInMemoryContext(dbName);
            var existing = await ctx.Courses
                .Include(c => c.Tags)
                .FirstAsync(c => c.Id == courseId);

            var updated = new Course
            {
                Id = courseId,
                CatalogId = catalogId,
                Title = "Test",
                Code = "T-001",
                Tags =
                [
                    new TopicTag { Id = tag1Id, Label = "Existing" },
                    new TopicTag { Id = tag2Id, Label = "New" }
                ]
            };

            ctx.InsertUpdateOrDeleteGraph(updated, existing);
            await ctx.SaveChangesAsync();
        }

        // Assert
        {
            await using var verifyCtx = CreateInMemoryContext(dbName);
            var result = await verifyCtx.Courses
                .Include(c => c.Tags)
                .FirstAsync(c => c.Id == courseId);
            result.Tags.Should().HaveCount(2);
        }
    }

    [Fact]
    public async Task Detects_removed_skip_navigation_entries()
    {
        var dbName = Guid.NewGuid().ToString();
        var catalogId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var tag1Id = Guid.NewGuid();
        var tag2Id = Guid.NewGuid();

        {
            await using var seedCtx = CreateInMemoryContext(dbName);
            seedCtx.LearningCatalogs.Add(new LearningCatalog { Id = catalogId, Name = "Catalog" });
            seedCtx.Courses.Add(new Course
            {
                Id = courseId,
                CatalogId = catalogId,
                Title = "Test",
                Code = "T-001",
                Tags =
                [
                    new TopicTag { Id = tag1Id, Label = "Keep" },
                    new TopicTag { Id = tag2Id, Label = "Remove" }
                ]
            });
            await seedCtx.SaveChangesAsync();
        }

        {
            await using var ctx = CreateInMemoryContext(dbName);
            var existing = await ctx.Courses
                .Include(c => c.Tags)
                .FirstAsync(c => c.Id == courseId);

            var updated = new Course
            {
                Id = courseId,
                CatalogId = catalogId,
                Title = "Test",
                Code = "T-001",
                Tags = [new TopicTag { Id = tag1Id, Label = "Keep" }]
            };

            ctx.InsertUpdateOrDeleteGraph(updated, existing);
            await ctx.SaveChangesAsync();
        }

        {
            await using var verifyCtx = CreateInMemoryContext(dbName);
            var result = await verifyCtx.Courses
                .Include(c => c.Tags)
                .FirstAsync(c => c.Id == courseId);
            result.Tags.Should().HaveCount(1);
            result.Tags.Single().Id.Should().Be(tag1Id);
        }
    }

    [Fact]
    public async Task Linking_existing_related_entity_does_not_clobber_modified_tracked_state()
    {
        var dbName = Guid.NewGuid().ToString();
        var catalogId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var tagId = Guid.NewGuid();

        {
            await using var seedCtx = CreateInMemoryContext(dbName);
            seedCtx.LearningCatalogs.Add(new LearningCatalog { Id = catalogId, Name = "Catalog" });
            seedCtx.TopicTags.Add(new TopicTag { Id = tagId, Label = "Persisted" });
            seedCtx.Courses.Add(new Course
            {
                Id = courseId,
                CatalogId = catalogId,
                Title = "Test",
                Code = "T-001",
                Tags = []
            });
            await seedCtx.SaveChangesAsync();
        }

        {
            await using var ctx = CreateInMemoryContext(dbName);
            var existing = await ctx.Courses
                .Include(c => c.Tags)
                .FirstAsync(c => c.Id == courseId);

            var trackedTag = await ctx.TopicTags.FirstAsync(t => t.Id == tagId);
            trackedTag.Label = "Locally Modified";
            ctx.ChangeTracker.DetectChanges();
            ctx.Entry(trackedTag).State.Should().Be(EntityState.Modified);

            var updated = new Course
            {
                Id = courseId,
                CatalogId = catalogId,
                Title = "Test",
                Code = "T-001",
                Tags = [new TopicTag { Id = tagId, Label = "Detached Copy" }]
            };

            ctx.InsertUpdateOrDeleteGraph(updated, existing);
            await ctx.SaveChangesAsync();
        }

        {
            await using var verifyCtx = CreateInMemoryContext(dbName);
            var result = await verifyCtx.Courses
                .Include(c => c.Tags)
                .FirstAsync(c => c.Id == courseId);

            result.Tags.Should().ContainSingle(t => t.Id == tagId);
            result.Tags.Single().Label.Should().Be("Locally Modified");
        }
    }

    [Fact]
    public async Task Already_linked_related_entity_does_not_clobber_modified_tracked_state()
    {
        var dbName = Guid.NewGuid().ToString();
        var catalogId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var tagId = Guid.NewGuid();

        {
            await using var seedCtx = CreateInMemoryContext(dbName);
            var tag = new TopicTag { Id = tagId, Label = "Persisted" };
            seedCtx.LearningCatalogs.Add(new LearningCatalog { Id = catalogId, Name = "Catalog" });
            seedCtx.Courses.Add(new Course
            {
                Id = courseId,
                CatalogId = catalogId,
                Title = "Test",
                Code = "T-001",
                Tags = [tag]
            });
            await seedCtx.SaveChangesAsync();
        }

        {
            await using var ctx = CreateInMemoryContext(dbName);
            var existing = await ctx.Courses
                .Include(c => c.Tags)
                .FirstAsync(c => c.Id == courseId);

            existing.Tags.Should().ContainSingle(t => t.Id == tagId);

            var trackedTag = existing.Tags.Single(t => t.Id == tagId);
            trackedTag.Label = "Locally Modified";
            ctx.ChangeTracker.DetectChanges();
            ctx.Entry(trackedTag).State.Should().Be(EntityState.Modified);

            var updated = new Course
            {
                Id = courseId,
                CatalogId = catalogId,
                Title = "Test",
                Code = "T-001",
                Tags = [new TopicTag { Id = tagId, Label = "Detached Copy" }]
            };

            ctx.InsertUpdateOrDeleteGraph(updated, existing);
            await ctx.SaveChangesAsync();
        }

        {
            await using var verifyCtx = CreateInMemoryContext(dbName);
            var result = await verifyCtx.Courses
                .Include(c => c.Tags)
                .FirstAsync(c => c.Id == courseId);

            result.Tags.Should().ContainSingle(t => t.Id == tagId);
            result.Tags.Single().Label.Should().Be("Locally Modified");
        }
    }

    [Fact]
    public async Task Detects_added_payload_association_entities()
    {
        // Arrange
        await using var ctx = CreateInMemoryContext();
        var mentor = new Mentor { Id = Guid.NewGuid(), DisplayName = "M1", Status = "Active" };
        var course = new Course
        {
            Id = Guid.NewGuid(),
            CatalogId = Guid.NewGuid(),
            Title = "Test",
            Code = "T-001",
            MentorAssignments = []
        };
        ctx.Mentors.Add(mentor);
        ctx.Courses.Add(course);
        await ctx.SaveChangesAsync();

        var existing = await ctx.Courses
            .Include(c => c.MentorAssignments)
            .FirstAsync(c => c.Id == course.Id);

        var updated = new Course
        {
            Id = course.Id,
            CatalogId = course.CatalogId,
            Title = "Test",
            Code = "T-001",
            MentorAssignments =
            [
                new CourseMentorAssignment
                {
                    CourseId = course.Id,
                    MentorId = mentor.Id,
                    Role = "Lead",
                    AssignedOnUtc = DateTime.UtcNow,
                    AllocationPercent = 50m
                }
            ]
        };

        // Act
        ctx.InsertUpdateOrDeleteGraph(updated, existing);
        await ctx.SaveChangesAsync();

        // Assert
        var assignments = await ctx.CourseMentorAssignments
            .Where(a => a.CourseId == course.Id)
            .ToListAsync();
        assignments.Should().HaveCount(1);
        assignments[0].Role.Should().Be("Lead");
    }
}
