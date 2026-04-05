using Diwink.Extensions.EntityFrameworkCore.Exceptions;
using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;
using Diwink.Extensions.EntityFrameworkCore.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Integration.Contracts.Rejection;

/// <summary>
/// Integration tests proving all-or-nothing rejection semantics (FR-017).
/// When a graph contains both supported and unsupported mutations, the entire
/// operation is rejected — no supported mutations are applied.
/// </summary>
public class PartialMutationNotAllowedTests : IntegrationTestBase
{
    public PartialMutationNotAllowedTests(SqlServerContainerFixture fixture)
        : base(fixture) { }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await using var ctx = CreateContext();
        await SeedData.SeedFullScenarioAsync(ctx);
    }

    [Fact]
    public async Task Mixed_supported_and_unsupported_mutations_rejects_entire_operation()
    {
        // Arrange — load LearningCatalog with both Courses (unsupported 1:M) and Tags (supported M:M)
        await using var ctx = CreateContext();
        var existing = await ctx.LearningCatalogs
            .Include(c => c.Courses)
            .Include(c => c.Tags)
            .FirstAsync(c => c.Id == SeedData.CatalogId);

        var originalName = existing.Name;

        // Updated graph has:
        // - Supported mutation: change catalog name (scalar)
        // - Supported mutation: modify tags (M:M)
        // - Unsupported mutation: add a course (1:M)
        var updated = new LearningCatalog
        {
            Id = SeedData.CatalogId,
            Name = "Should NOT be applied",
            Tags = existing.Tags.Select(t => new TopicTag
            {
                Id = t.Id,
                Label = t.Label
            }).ToList(),
            Courses =
            [
                ..existing.Courses.Select(c => new Course
                {
                    Id = c.Id,
                    CatalogId = c.CatalogId,
                    Title = c.Title,
                    Code = c.Code
                }),
                new Course
                {
                    Id = Guid.NewGuid(),
                    CatalogId = SeedData.CatalogId,
                    Title = "Injected Course",
                    Code = "INJ-001"
                }
            ]
        };

        // Act & Assert — entire operation rejected
        var act = () => ctx.UpdateGraph(updated, existing);
        act.Should().Throw<GraphUpdateException>();

        // Verify no mutations were applied (all-or-nothing)
        await using var verifyCtx = CreateContext();
        var result = await verifyCtx.LearningCatalogs
            .FirstAsync(c => c.Id == SeedData.CatalogId);
        result.Name.Should().Be(originalName,
            "scalar updates must NOT be applied when unsupported mutation causes rejection");
    }
}
