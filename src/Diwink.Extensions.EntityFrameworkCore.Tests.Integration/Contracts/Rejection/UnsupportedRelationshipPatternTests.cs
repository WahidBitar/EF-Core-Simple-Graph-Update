using Diwink.Extensions.EntityFrameworkCore.Exceptions;
using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;
using Diwink.Extensions.EntityFrameworkCore.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Integration.Contracts.Rejection;

/// <summary>
/// Integration tests proving that unsupported relationship types are rejected
/// when mutations are detected, and silently skipped when unchanged (FR-018, FR-019).
/// </summary>
public class UnsupportedRelationshipPatternTests : IntegrationTestBase
{
    public UnsupportedRelationshipPatternTests(SqlServerContainerFixture fixture)
        : base(fixture) { }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await using var ctx = CreateContext();
        await SeedData.SeedFullScenarioAsync(ctx);
    }

    [Fact]
    public async Task Unchanged_one_to_many_navigation_is_silently_skipped()
    {
        // Arrange — load LearningCatalog with its one-to-many Courses
        await using var ctx = CreateContext();
        var existing = await ctx.LearningCatalogs
            .Include(c => c.Courses)
            .FirstAsync(c => c.Id == SeedData.CatalogId);

        // Updated graph keeps same courses (no mutation in unsupported nav)
        var updated = new LearningCatalog
        {
            Id = SeedData.CatalogId,
            Name = "Updated Name",
            Courses = existing.Courses.Select(c => new Course
            {
                Id = c.Id,
                CatalogId = c.CatalogId,
                Title = c.Title,
                Code = c.Code
            }).ToList()
        };

        // Act — should NOT throw because one-to-many is unchanged
        ctx.UpdateGraph(updated, existing);
        await ctx.SaveChangesAsync();

        // Assert — scalar update applied, courses unchanged
        await using var verifyCtx = CreateContext();
        var result = await verifyCtx.LearningCatalogs
            .Include(c => c.Courses)
            .FirstAsync(c => c.Id == SeedData.CatalogId);
        result.Name.Should().Be("Updated Name");
        result.Courses.Should().HaveCount(2);
    }

    [Fact]
    public async Task Mutated_one_to_many_navigation_is_rejected()
    {
        // Arrange — load LearningCatalog with its one-to-many Courses
        await using var ctx = CreateContext();
        var existing = await ctx.LearningCatalogs
            .Include(c => c.Courses)
            .FirstAsync(c => c.Id == SeedData.CatalogId);

        // Updated graph adds a new course (mutation in unsupported nav)
        var updated = new LearningCatalog
        {
            Id = SeedData.CatalogId,
            Name = "Updated Name",
            Courses =
            [
                new Course
                {
                    Id = SeedData.Course1Id,
                    CatalogId = SeedData.CatalogId,
                    Title = "Software Design",
                    Code = "SD-101"
                },
                new Course
                {
                    Id = SeedData.Course2Id,
                    CatalogId = SeedData.CatalogId,
                    Title = "Security Patterns",
                    Code = "SP-201"
                },
                new Course
                {
                    Id = Guid.NewGuid(),
                    CatalogId = SeedData.CatalogId,
                    Title = "New Course",
                    Code = "NC-301"
                }
            ]
        };

        // Act & Assert — should throw because one-to-many has mutations
        var act = () => ctx.UpdateGraph(updated, existing);
        act.Should().Throw<UnsupportedNavigationMutatedException>()
            .Which.RelationshipType.Should().Be("OneToMany");
    }

    [Fact]
    public async Task Mutated_one_to_many_removal_is_rejected()
    {
        // Arrange — load LearningCatalog with its one-to-many Courses
        await using var ctx = CreateContext();
        var existing = await ctx.LearningCatalogs
            .Include(c => c.Courses)
            .FirstAsync(c => c.Id == SeedData.CatalogId);

        // Updated graph removes one course (mutation in unsupported nav)
        var updated = new LearningCatalog
        {
            Id = SeedData.CatalogId,
            Name = existing.Name,
            Courses =
            [
                new Course
                {
                    Id = SeedData.Course1Id,
                    CatalogId = SeedData.CatalogId,
                    Title = "Software Design",
                    Code = "SD-101"
                }
            ]
        };

        // Act & Assert
        var act = () => ctx.UpdateGraph(updated, existing);
        act.Should().Throw<UnsupportedNavigationMutatedException>();
    }

    [Fact]
    public async Task In_place_scalar_edit_in_unsupported_one_to_many_is_rejected()
    {
        await using var ctx = CreateContext();
        var existing = await ctx.LearningCatalogs
            .Include(c => c.Courses)
            .FirstAsync(c => c.Id == SeedData.CatalogId);

        var updated = new LearningCatalog
        {
            Id = SeedData.CatalogId,
            Name = existing.Name,
            Courses = existing.Courses.Select(c => new Course
            {
                Id = c.Id,
                CatalogId = c.CatalogId,
                Title = c.Id == SeedData.Course1Id ? "Retitled Course" : c.Title,
                Code = c.Code
            }).ToList()
        };

        var act = () => ctx.UpdateGraph(updated, existing);

        act.Should().Throw<UnsupportedNavigationMutatedException>()
            .Which.RelationshipPath.Should().Be("LearningCatalog.Courses");
    }
}
