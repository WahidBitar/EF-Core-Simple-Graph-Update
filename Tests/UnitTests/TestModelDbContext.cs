using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace UnitTests;

public class TestModelDbContext : DbContext
{
    public TestModelDbContext(DbContextOptions<TestModelDbContext> options) : base(options)
    {
    }

    public DbSet<AggregateA> AggregateAs { get; set; }
    public DbSet<AggregateB> AggregateBs { get; set; }

    public DbSet<CompositeEntity> CompositeEntities { get; set; }
    public DbSet<EntityA> EntityAs { get; set; }
    public DbSet<EntityB> EntityBs { get; set; }
    public DbSet<EntityC> EntityCs { get; set; }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<AggregateA>(config);
        builder.Entity<AggregateB>(config);
        builder.Entity<CompositeEntity>(config);
        builder.Entity<EntityA>(config);
        builder.Entity<EntityB>(config);
        builder.Entity<EntityC>(config);
    }


    private void config(EntityTypeBuilder<AggregateA> builder)
    {
        builder.ToTable("AggregateAs");

        builder.HasMany(x => x.RelatedAggregates)
            .WithOne(x => x.RelatedAggregate)
            .HasForeignKey(x => x.AggregateAId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.CompositeEntities)
            .WithOne(x => x.RelatedAggregate)
            .HasForeignKey(x => x.AggregateAId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private void config(EntityTypeBuilder<AggregateB> builder)
    {
        builder.ToTable("AggregateBs");
        builder.HasOne(x => x.OneToOneSubEntity)
            .WithOne()
            .HasForeignKey<EntityC>(x => x.AggregateBId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ManySubEntities)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }

    private void config(EntityTypeBuilder<CompositeEntity> builder)
    {
        builder.ToTable("CompositeEntities");
        builder.HasKey(x => new { x.AggregateAId, x.SomeId });
    }

    private void config(EntityTypeBuilder<EntityA> builder)
    {
        builder.ToTable("EntityAs");

        builder.OwnsOne(x => x.OptionalOwnedObject);
        builder.OwnsMany(x => x.ManyOwnedObjects);

        builder.HasMany(x => x.ManyToManyEntitiesB)
            .WithMany(x => x.ManyToManyEntitiesA);
    }

    private void config(EntityTypeBuilder<EntityB> builder)
    {
        builder.ToTable("EntityBs");
    }

    private void config(EntityTypeBuilder<EntityC> builder)
    {
        builder.ToTable("EntityCs");
        builder.HasKey(x => x.AggregateBId);
        builder.OwnsOne(x => x.RequiredOwnedObject);
    }
}