using Diwink.Extensions.EntityFrameworkCore.Exceptions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Unit.GraphDiff;

public class NestedNavigationValidationTests
{
    private static RecursiveGraphContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<RecursiveGraphContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new RecursiveGraphContext(options);
    }

    [Fact]
    public async Task Nested_unsupported_one_to_many_mutation_is_rejected()
    {
        var dbName = Guid.NewGuid().ToString();
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        {
            await using var seedCtx = CreateInMemoryContext(dbName);
            seedCtx.Roots.Add(new RecursiveRoot
            {
                Id = rootId,
                Name = "Root",
                Child = new RecursiveChild
                {
                    Id = childId,
                    RootId = rootId,
                    Name = "Child",
                    Items =
                    [
                        new RecursiveChildItem
                        {
                            Id = itemId,
                            ChildId = childId,
                            Value = "Keep"
                        }
                    ]
                }
            });

            await seedCtx.SaveChangesAsync();
        }

        {
            await using var ctx = CreateInMemoryContext(dbName);
            var existing = await ctx.Roots
                .Include(r => r.Child)
                .ThenInclude(c => c!.Items)
                .FirstAsync(r => r.Id == rootId);

            var updated = new RecursiveRoot
            {
                Id = rootId,
                Name = "Root",
                Child = new RecursiveChild
                {
                    Id = childId,
                    RootId = rootId,
                    Name = "Child",
                    Items =
                    [
                        new RecursiveChildItem
                        {
                            Id = itemId,
                            ChildId = childId,
                            Value = "Changed"
                        }
                    ]
                }
            };

            var act = () => ctx.UpdateGraph(updated, existing);

            act.Should().Throw<UnsupportedNavigationMutatedException>()
                .Which.RelationshipPath.Should().Be("RecursiveChild.Items");
        }
    }

    [Fact]
    public async Task Nested_unloaded_reference_mutation_is_rejected()
    {
        var dbName = Guid.NewGuid().ToString();
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var metadataId = Guid.NewGuid();

        {
            await using var seedCtx = CreateInMemoryContext(dbName);
            seedCtx.Roots.Add(new RecursiveRoot
            {
                Id = rootId,
                Name = "Root",
                Child = new RecursiveChild
                {
                    Id = childId,
                    RootId = rootId,
                    Name = "Child",
                    Metadata = new RecursiveChildMetadata
                    {
                        Id = metadataId,
                        ChildId = childId,
                        Notes = "Existing"
                    }
                }
            });

            await seedCtx.SaveChangesAsync();
        }

        {
            await using var ctx = CreateInMemoryContext(dbName);
            var existing = await ctx.Roots
                .Include(r => r.Child)
                .FirstAsync(r => r.Id == rootId);

            var updated = new RecursiveRoot
            {
                Id = rootId,
                Name = "Root",
                Child = new RecursiveChild
                {
                    Id = childId,
                    RootId = rootId,
                    Name = "Child",
                    Metadata = new RecursiveChildMetadata
                    {
                        Id = metadataId,
                        ChildId = childId,
                        Notes = "Changed"
                    }
                }
            };

            var act = () => ctx.UpdateGraph(updated, existing);

            act.Should().Throw<UnloadedNavigationMutationException>()
                .Which.NavigationName.Should().Be("Metadata");
        }
    }

    [Fact]
    public async Task Nested_optional_reference_removal_detaches_dependent()
    {
        var dbName = Guid.NewGuid().ToString();
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var metadataId = Guid.NewGuid();

        {
            await using var seedCtx = CreateInMemoryContext(dbName);
            seedCtx.Roots.Add(new RecursiveRoot
            {
                Id = rootId,
                Name = "Root",
                Child = new RecursiveChild
                {
                    Id = childId,
                    RootId = rootId,
                    Name = "Child",
                    Metadata = new RecursiveChildMetadata
                    {
                        Id = metadataId,
                        ChildId = childId,
                        Notes = "Existing"
                    }
                }
            });

            await seedCtx.SaveChangesAsync();
        }

        {
            await using var ctx = CreateInMemoryContext(dbName);
            var existing = await ctx.Roots
                .Include(r => r.Child)
                .ThenInclude(c => c!.Metadata)
                .FirstAsync(r => r.Id == rootId);

            var updated = new RecursiveRoot
            {
                Id = rootId,
                Name = "Root",
                Child = new RecursiveChild
                {
                    Id = childId,
                    RootId = rootId,
                    Name = "Child",
                    Metadata = null
                }
            };

            ctx.UpdateGraph(updated, existing);
            await ctx.SaveChangesAsync();
        }

        {
            await using var verifyCtx = CreateInMemoryContext(dbName);
            var root = await verifyCtx.Roots
                .Include(r => r.Child)
                .ThenInclude(c => c!.Metadata)
                .FirstAsync(r => r.Id == rootId);

            root.Child.Should().NotBeNull();
            root.Child!.Metadata.Should().BeNull();

            var metadata = await verifyCtx.ChildMetadata.FirstOrDefaultAsync(m => m.Id == metadataId);
            metadata.Should().NotBeNull();
            metadata!.ChildId.Should().BeNull();
        }
    }

    [Fact]
    public async Task Nested_many_to_many_removal_unlinks_without_deleting_related_entity()
    {
        var dbName = Guid.NewGuid().ToString();
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var tag1Id = Guid.NewGuid();
        var tag2Id = Guid.NewGuid();

        {
            await using var seedCtx = CreateInMemoryContext(dbName);
            seedCtx.Roots.Add(new RecursiveRoot
            {
                Id = rootId,
                Name = "Root",
                Child = new RecursiveChild
                {
                    Id = childId,
                    RootId = rootId,
                    Name = "Child",
                    Tags =
                    [
                        new RecursiveTag { Id = tag1Id, Label = "Keep" },
                        new RecursiveTag { Id = tag2Id, Label = "Remove" }
                    ]
                }
            });

            await seedCtx.SaveChangesAsync();
        }

        {
            await using var ctx = CreateInMemoryContext(dbName);
            var existing = await ctx.Roots
                .Include(r => r.Child)
                .ThenInclude(c => c!.Tags)
                .FirstAsync(r => r.Id == rootId);

            var updated = new RecursiveRoot
            {
                Id = rootId,
                Name = "Root",
                Child = new RecursiveChild
                {
                    Id = childId,
                    RootId = rootId,
                    Name = "Child",
                    Tags =
                    [
                        new RecursiveTag { Id = tag1Id, Label = "Keep" }
                    ]
                }
            };

            ctx.UpdateGraph(updated, existing);
            await ctx.SaveChangesAsync();
        }

        {
            await using var verifyCtx = CreateInMemoryContext(dbName);
            var root = await verifyCtx.Roots
                .Include(r => r.Child)
                .ThenInclude(c => c!.Tags)
                .FirstAsync(r => r.Id == rootId);

            root.Child.Should().NotBeNull();
            root.Child!.Tags.Select(t => t.Id).Should().ContainSingle().Which.Should().Be(tag1Id);
            (await verifyCtx.Tags.AnyAsync(t => t.Id == tag2Id)).Should().BeTrue();
        }
    }
}

internal sealed class RecursiveGraphContext : DbContext
{
    public RecursiveGraphContext(DbContextOptions<RecursiveGraphContext> options)
        : base(options)
    {
    }

    public DbSet<RecursiveRoot> Roots => Set<RecursiveRoot>();
    public DbSet<RecursiveChild> Children => Set<RecursiveChild>();
    public DbSet<RecursiveChildItem> ChildItems => Set<RecursiveChildItem>();
    public DbSet<RecursiveChildMetadata> ChildMetadata => Set<RecursiveChildMetadata>();
    public DbSet<RecursiveTag> Tags => Set<RecursiveTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecursiveRoot>()
            .HasOne(r => r.Child)
            .WithOne()
            .HasForeignKey<RecursiveChild>(c => c.RootId)
            .IsRequired();

        modelBuilder.Entity<RecursiveChild>()
            .HasMany(c => c.Items)
            .WithOne()
            .HasForeignKey(i => i.ChildId);

        modelBuilder.Entity<RecursiveChild>()
            .HasOne(c => c.Metadata)
            .WithOne()
            .HasForeignKey<RecursiveChildMetadata>(m => m.ChildId)
            .IsRequired(false);

        modelBuilder.Entity<RecursiveChild>()
            .HasMany(c => c.Tags)
            .WithMany();
    }
}

internal sealed class RecursiveRoot
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public RecursiveChild? Child { get; set; }
}

internal sealed class RecursiveChild
{
    public Guid Id { get; set; }
    public Guid RootId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<RecursiveChildItem> Items { get; set; } = [];
    public RecursiveChildMetadata? Metadata { get; set; }
    public ICollection<RecursiveTag> Tags { get; set; } = [];
}

internal sealed class RecursiveChildItem
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public string Value { get; set; } = string.Empty;
}

internal sealed class RecursiveChildMetadata
{
    public Guid Id { get; set; }
    public Guid? ChildId { get; set; }
    public string Notes { get; set; } = string.Empty;
}

internal sealed class RecursiveTag
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
}
