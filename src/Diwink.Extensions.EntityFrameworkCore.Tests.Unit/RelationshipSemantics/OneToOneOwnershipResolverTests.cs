using Diwink.Extensions.EntityFrameworkCore.TestModel;
using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Unit.RelationshipSemantics;

/// <summary>
/// Unit tests for one-to-one ownership resolver and removal strategy selection.
/// Uses InMemory provider for fast isolated testing of strategy mechanics.
/// </summary>
public class OneToOneOwnershipResolverTests
{
    private static TestDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(options);
    }

    [Fact]
    public async Task Required_one_to_one_removal_deletes_dependent()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var courseId = Guid.NewGuid();
        var catalogId = Guid.NewGuid();

        // Seed
        {
            await using var seedCtx = CreateInMemoryContext(dbName);
            var catalog = new LearningCatalog { Id = catalogId, Name = "Cat" };
            var course = new Course
            {
                Id = courseId,
                CatalogId = catalogId,
                Title = "Test",
                Code = "T-001",
                Policy = new CoursePolicy
                {
                    CourseId = courseId,
                    PolicyVersion = "1.0",
                    IsMandatory = true
                }
            };
            seedCtx.LearningCatalogs.Add(catalog);
            seedCtx.Courses.Add(course);
            await seedCtx.SaveChangesAsync();
        }

        // Act
        {
            await using var ctx = CreateInMemoryContext(dbName);
            var existing = await ctx.Courses
                .Include(c => c.Policy)
                .FirstAsync(c => c.Id == courseId);

            var updated = new Course
            {
                Id = courseId,
                CatalogId = catalogId,
                Title = "Test",
                Code = "T-001",
                Policy = null
            };

            ctx.UpdateGraph(updated, existing);
            await ctx.SaveChangesAsync();
        }

        // Assert
        {
            await using var verifyCtx = CreateInMemoryContext(dbName);
            var course = await verifyCtx.Courses
                .Include(c => c.Policy)
                .FirstAsync(c => c.Id == courseId);
            course.Policy.Should().BeNull();

            var policyExists = await verifyCtx.Set<CoursePolicy>()
                .AnyAsync(p => p.CourseId == courseId);
            policyExists.Should().BeFalse();
        }
    }

    [Fact]
    public async Task Optional_one_to_one_removal_nulls_fk()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var mentorId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();

        // Seed
        {
            await using var seedCtx = CreateInMemoryContext(dbName);
            var mentor = new Mentor
            {
                Id = mentorId,
                DisplayName = "M1",
                Status = "Active",
                Workspace = new MentorWorkspace
                {
                    Id = workspaceId,
                    MentorId = mentorId,
                    DeskCode = "D-100",
                    Building = "HQ"
                }
            };
            seedCtx.Mentors.Add(mentor);
            await seedCtx.SaveChangesAsync();
        }

        // Act
        {
            await using var ctx = CreateInMemoryContext(dbName);
            var existing = await ctx.Mentors
                .Include(m => m.Workspace)
                .FirstAsync(m => m.Id == mentorId);

            var updated = new Mentor
            {
                Id = mentorId,
                DisplayName = "M1",
                Status = "Active",
                Workspace = null
            };

            ctx.UpdateGraph(updated, existing);
            await ctx.SaveChangesAsync();
        }

        // Assert
        {
            await using var verifyCtx = CreateInMemoryContext(dbName);
            var mentor = await verifyCtx.Mentors
                .Include(m => m.Workspace)
                .FirstAsync(m => m.Id == mentorId);
            mentor.Workspace.Should().BeNull();

            var workspace = await verifyCtx.Set<MentorWorkspace>()
                .FirstOrDefaultAsync(w => w.Id == workspaceId);
            workspace.Should().NotBeNull("optional dependent should be preserved");
            workspace!.MentorId.Should().BeNull("FK should be nulled");
        }
    }

    [Fact]
    public async Task Optional_one_to_one_update_with_same_key_updates_in_place()
    {
        var dbName = Guid.NewGuid().ToString();
        var mentorId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();

        {
            await using var seedCtx = CreateInMemoryContext(dbName);
            seedCtx.Mentors.Add(new Mentor
            {
                Id = mentorId,
                DisplayName = "M1",
                Status = "Active",
                Workspace = new MentorWorkspace
                {
                    Id = workspaceId,
                    MentorId = mentorId,
                    DeskCode = "D-100",
                    Building = "HQ"
                }
            });
            await seedCtx.SaveChangesAsync();
        }

        {
            await using var ctx = CreateInMemoryContext(dbName);
            var existing = await ctx.Mentors
                .Include(m => m.Workspace)
                .FirstAsync(m => m.Id == mentorId);

            var updated = new Mentor
            {
                Id = mentorId,
                DisplayName = "M1",
                Status = "Active",
                Workspace = new MentorWorkspace
                {
                    Id = workspaceId,
                    MentorId = mentorId,
                    DeskCode = "D-200",
                    Building = "Annex"
                }
            };

            ctx.UpdateGraph(updated, existing);
            await ctx.SaveChangesAsync();
        }

        {
            await using var verifyCtx = CreateInMemoryContext(dbName);
            var workspaces = await verifyCtx.Set<MentorWorkspace>().ToListAsync();
            workspaces.Should().HaveCount(1);
            workspaces[0].Id.Should().Be(workspaceId);
            workspaces[0].DeskCode.Should().Be("D-200");
            workspaces[0].Building.Should().Be("Annex");
            workspaces[0].MentorId.Should().Be(mentorId);
        }
    }

    [Fact]
    public async Task Optional_one_to_one_add_when_missing_inserts_and_links_new_dependent()
    {
        var dbName = Guid.NewGuid().ToString();
        var mentorId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();

        {
            await using var seedCtx = CreateInMemoryContext(dbName);
            seedCtx.Mentors.Add(new Mentor
            {
                Id = mentorId,
                DisplayName = "M1",
                Status = "Active"
            });
            await seedCtx.SaveChangesAsync();
        }

        {
            await using var ctx = CreateInMemoryContext(dbName);
            var existing = await ctx.Mentors
                .Include(m => m.Workspace)
                .FirstAsync(m => m.Id == mentorId);

            var updated = new Mentor
            {
                Id = mentorId,
                DisplayName = "M1",
                Status = "Active",
                Workspace = new MentorWorkspace
                {
                    Id = workspaceId,
                    MentorId = mentorId,
                    DeskCode = "D-300",
                    Building = "West"
                }
            };

            ctx.UpdateGraph(updated, existing);
            await ctx.SaveChangesAsync();
        }

        {
            await using var verifyCtx = CreateInMemoryContext(dbName);
            var mentor = await verifyCtx.Mentors
                .Include(m => m.Workspace)
                .FirstAsync(m => m.Id == mentorId);

            mentor.Workspace.Should().NotBeNull();
            mentor.Workspace!.Id.Should().Be(workspaceId);
            mentor.Workspace.MentorId.Should().Be(mentorId);
            mentor.Workspace.DeskCode.Should().Be("D-300");
        }
    }

    [Fact]
    public async Task Optional_one_to_one_replace_detaches_old_and_links_new_dependent()
    {
        var dbName = Guid.NewGuid().ToString();
        var mentorId = Guid.NewGuid();
        var oldWorkspaceId = Guid.NewGuid();
        var newWorkspaceId = Guid.NewGuid();

        {
            await using var seedCtx = CreateInMemoryContext(dbName);
            seedCtx.Mentors.Add(new Mentor
            {
                Id = mentorId,
                DisplayName = "M1",
                Status = "Active",
                Workspace = new MentorWorkspace
                {
                    Id = oldWorkspaceId,
                    MentorId = mentorId,
                    DeskCode = "D-100",
                    Building = "HQ"
                }
            });
            await seedCtx.SaveChangesAsync();
        }

        {
            await using var ctx = CreateInMemoryContext(dbName);
            var existing = await ctx.Mentors
                .Include(m => m.Workspace)
                .FirstAsync(m => m.Id == mentorId);

            var updated = new Mentor
            {
                Id = mentorId,
                DisplayName = "M1",
                Status = "Active",
                Workspace = new MentorWorkspace
                {
                    Id = newWorkspaceId,
                    MentorId = mentorId,
                    DeskCode = "D-999",
                    Building = "Tower"
                }
            };

            ctx.UpdateGraph(updated, existing);
            await ctx.SaveChangesAsync();
        }

        {
            await using var verifyCtx = CreateInMemoryContext(dbName);
            var mentor = await verifyCtx.Mentors
                .Include(m => m.Workspace)
                .FirstAsync(m => m.Id == mentorId);
            mentor.Workspace.Should().NotBeNull();
            mentor.Workspace!.Id.Should().Be(newWorkspaceId);
            mentor.Workspace.MentorId.Should().Be(mentorId);

            var oldWorkspace = await verifyCtx.Set<MentorWorkspace>()
                .FirstAsync(w => w.Id == oldWorkspaceId);
            oldWorkspace.MentorId.Should().BeNull();

            var allWorkspaces = await verifyCtx.Set<MentorWorkspace>().ToListAsync();
            allWorkspaces.Should().HaveCount(2);
        }
    }

    [Fact]
    public async Task Update_required_dependent_properties()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var courseId = Guid.NewGuid();
        var catalogId = Guid.NewGuid();

        {
            await using var seedCtx = CreateInMemoryContext(dbName);
            var catalog = new LearningCatalog { Id = catalogId, Name = "Cat" };
            var course = new Course
            {
                Id = courseId,
                CatalogId = catalogId,
                Title = "Test",
                Code = "T-001",
                Policy = new CoursePolicy
                {
                    CourseId = courseId,
                    PolicyVersion = "1.0",
                    IsMandatory = true
                }
            };
            seedCtx.LearningCatalogs.Add(catalog);
            seedCtx.Courses.Add(course);
            await seedCtx.SaveChangesAsync();
        }

        // Act
        {
            await using var ctx = CreateInMemoryContext(dbName);
            var existing = await ctx.Courses
                .Include(c => c.Policy)
                .FirstAsync(c => c.Id == courseId);

            var updated = new Course
            {
                Id = courseId,
                CatalogId = catalogId,
                Title = "Test",
                Code = "T-001",
                Policy = new CoursePolicy
                {
                    CourseId = courseId,
                    PolicyVersion = "2.0",
                    IsMandatory = false
                }
            };

            ctx.UpdateGraph(updated, existing);
            await ctx.SaveChangesAsync();
        }

        // Assert
        {
            await using var verifyCtx = CreateInMemoryContext(dbName);
            var policy = await verifyCtx.Set<CoursePolicy>()
                .FirstAsync(p => p.CourseId == courseId);
            policy.PolicyVersion.Should().Be("2.0");
            policy.IsMandatory.Should().BeFalse();
        }
    }
}
