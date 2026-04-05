using Diwink.Extensions.EntityFrameworkCore.GraphUpdate;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Unit.GraphDiff;

public class RelationshipClassificationTests
{
    [Fact]
    public void Explicit_join_with_payload_is_classified_as_payload_many_to_many()
    {
        using var context = CreateContext();
        var navigation = context.Model
            .FindEntityType(typeof(JoinRoot))!
            .FindNavigation(nameof(JoinRoot.PayloadLinks))!;

        GraphUpdateOrchestrator.ClassifyNavigation(navigation)
            .Should().Be(NavigationClassification.PayloadManyToMany);
    }

    [Fact]
    public void Explicit_join_without_payload_is_not_classified_as_payload_many_to_many()
    {
        using var context = CreateContext();
        var navigation = context.Model
            .FindEntityType(typeof(JoinRoot))!
            .FindNavigation(nameof(JoinRoot.PureLinks))!;

        GraphUpdateOrchestrator.ClassifyNavigation(navigation)
            .Should().Be(NavigationClassification.Unsupported);
    }

    private static JoinClassificationContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<JoinClassificationContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new JoinClassificationContext(options);
    }

    private sealed class JoinClassificationContext : DbContext
    {
        public JoinClassificationContext(DbContextOptions<JoinClassificationContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<JoinRoot>()
                .HasMany(root => root.PayloadLinks)
                .WithOne(link => link.Root)
                .HasForeignKey(link => link.RootId);

            modelBuilder.Entity<PayloadLink>()
                .HasKey(link => new { link.RootId, link.TagId });

            modelBuilder.Entity<PayloadLink>()
                .HasOne(link => link.Tag)
                .WithMany()
                .HasForeignKey(link => link.TagId);

            modelBuilder.Entity<JoinRoot>()
                .HasMany(root => root.PureLinks)
                .WithOne(link => link.Root)
                .HasForeignKey(link => link.RootId);

            modelBuilder.Entity<PureLink>()
                .HasKey(link => new { link.RootId, link.TagId });

            modelBuilder.Entity<PureLink>()
                .HasOne(link => link.Tag)
                .WithMany()
                .HasForeignKey(link => link.TagId);
        }
    }

    private sealed class JoinRoot
    {
        public int Id { get; set; }
        public ICollection<PayloadLink> PayloadLinks { get; set; } = [];
        public ICollection<PureLink> PureLinks { get; set; } = [];
    }

    private sealed class JoinTag
    {
        public int Id { get; set; }
    }

    private sealed class PayloadLink
    {
        public int RootId { get; set; }
        public int TagId { get; set; }
        public string Notes { get; set; } = string.Empty;
        public JoinRoot Root { get; set; } = null!;
        public JoinTag Tag { get; set; } = null!;
    }

    private sealed class PureLink
    {
        public int RootId { get; set; }
        public int TagId { get; set; }
        public JoinRoot Root { get; set; } = null!;
        public JoinTag Tag { get; set; } = null!;
    }
}
