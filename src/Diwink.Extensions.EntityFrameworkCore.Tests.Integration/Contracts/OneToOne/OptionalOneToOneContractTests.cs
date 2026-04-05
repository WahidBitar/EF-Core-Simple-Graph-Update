using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;
using Diwink.Extensions.EntityFrameworkCore.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Integration.Contracts.OneToOne;

/// <summary>
/// Integration contract tests for optional one-to-one (Mentor -> MentorWorkspace).
/// Optional dependent removal => null FK (detach), NOT delete (FR-009, FR-010, FR-011).
/// MentorWorkspace has nullable MentorId FK with SetNull delete behavior.
/// </summary>
public class OptionalOneToOneContractTests : IntegrationTestBase
{
    public OptionalOneToOneContractTests(SqlServerContainerFixture fixture)
        : base(fixture) { }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await using var ctx = CreateContext();
        await SeedData.SeedFullScenarioAsync(ctx);
    }

    [Fact]
    public async Task Remove_optional_dependent_nulls_fk_preserves_entity()
    {
        // Arrange — Mentor1 has Workspace1
        await using var ctx = CreateContext();
        var existing = await ctx.Mentors
            .Include(m => m.Workspace)
            .FirstAsync(m => m.Id == SeedData.Mentor1Id);

        existing.Workspace.Should().NotBeNull("seed data includes workspace for Mentor1");

        // Updated graph removes the Workspace reference
        var updated = new Mentor
        {
            Id = SeedData.Mentor1Id,
            DisplayName = existing.DisplayName,
            Status = existing.Status,
            Workspace = null
        };

        // Act
        ctx.UpdateGraph(updated, existing);
        await ctx.SaveChangesAsync();

        // Assert — workspace should still exist but with null MentorId
        await using var verifyCtx = CreateContext();
        var mentor = await verifyCtx.Mentors
            .Include(m => m.Workspace)
            .FirstAsync(m => m.Id == SeedData.Mentor1Id);
        mentor.Workspace.Should().BeNull("reference should be removed from mentor");

        var workspace = await verifyCtx.Set<MentorWorkspace>()
            .FirstOrDefaultAsync(w => w.Id == SeedData.Workspace1Id);
        workspace.Should().NotBeNull("optional dependent should be preserved, not deleted");
        workspace!.MentorId.Should().BeNull("FK should be nulled for optional one-to-one");
    }

    [Fact]
    public async Task Update_optional_dependent_scalar_properties()
    {
        // Arrange
        await using var ctx = CreateContext();
        var existing = await ctx.Mentors
            .Include(m => m.Workspace)
            .FirstAsync(m => m.Id == SeedData.Mentor1Id);

        var updated = new Mentor
        {
            Id = SeedData.Mentor1Id,
            DisplayName = existing.DisplayName,
            Status = existing.Status,
            Workspace = new MentorWorkspace
            {
                Id = SeedData.Workspace1Id,
                MentorId = SeedData.Mentor1Id,
                DeskCode = "D-999",
                Building = "Annex-B"
            }
        };

        // Act
        ctx.UpdateGraph(updated, existing);
        await ctx.SaveChangesAsync();

        // Assert
        await using var verifyCtx = CreateContext();
        var workspace = await verifyCtx.Set<MentorWorkspace>()
            .FirstAsync(w => w.Id == SeedData.Workspace1Id);
        workspace.DeskCode.Should().Be("D-999");
        workspace.Building.Should().Be("Annex-B");
    }

    [Fact]
    public async Task Add_optional_dependent_when_none_exists()
    {
        // Arrange — Mentor2 has no workspace
        await using var ctx = CreateContext();
        var existing = await ctx.Mentors
            .Include(m => m.Workspace)
            .FirstAsync(m => m.Id == SeedData.Mentor2Id);

        existing.Workspace.Should().BeNull("seed data has no workspace for Mentor2");

        var newWorkspaceId = Guid.NewGuid();
        var updated = new Mentor
        {
            Id = SeedData.Mentor2Id,
            DisplayName = existing.DisplayName,
            Status = existing.Status,
            Workspace = new MentorWorkspace
            {
                Id = newWorkspaceId,
                MentorId = SeedData.Mentor2Id,
                DeskCode = "D-200",
                Building = "West Wing"
            }
        };

        // Act
        ctx.UpdateGraph(updated, existing);
        await ctx.SaveChangesAsync();

        // Assert
        await using var verifyCtx = CreateContext();
        var mentor = await verifyCtx.Mentors
            .Include(m => m.Workspace)
            .FirstAsync(m => m.Id == SeedData.Mentor2Id);
        mentor.Workspace.Should().NotBeNull();
        mentor.Workspace!.DeskCode.Should().Be("D-200");
        mentor.Workspace.Building.Should().Be("West Wing");
    }

    [Fact]
    public async Task Replace_optional_dependent_nulls_old_inserts_new()
    {
        // Arrange — Mentor1 has Workspace1, replace with a brand new workspace
        await using var ctx = CreateContext();
        var existing = await ctx.Mentors
            .Include(m => m.Workspace)
            .FirstAsync(m => m.Id == SeedData.Mentor1Id);

        var replacementId = Guid.NewGuid();
        var updated = new Mentor
        {
            Id = SeedData.Mentor1Id,
            DisplayName = existing.DisplayName,
            Status = existing.Status,
            Workspace = new MentorWorkspace
            {
                Id = replacementId,
                MentorId = SeedData.Mentor1Id,
                DeskCode = "D-NEW",
                Building = "Tower"
            }
        };

        // Act
        ctx.UpdateGraph(updated, existing);
        await ctx.SaveChangesAsync();

        // Assert — old workspace should be detached (FK nulled), new one linked
        await using var verifyCtx = CreateContext();
        var mentor = await verifyCtx.Mentors
            .Include(m => m.Workspace)
            .FirstAsync(m => m.Id == SeedData.Mentor1Id);
        mentor.Workspace.Should().NotBeNull();
        mentor.Workspace!.Id.Should().Be(replacementId);

        var oldWorkspace = await verifyCtx.Set<MentorWorkspace>()
            .FirstOrDefaultAsync(w => w.Id == SeedData.Workspace1Id);
        oldWorkspace.Should().NotBeNull("old dependent should be preserved");
        oldWorkspace!.MentorId.Should().BeNull("old FK should be nulled");
    }
}
