using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diwink.Extensions.EntityFrameworkCore.TestModel.Configurations;

public class LearningCatalogConfiguration : IEntityTypeConfiguration<LearningCatalog>
{
    /// <summary>
    /// Configures the EF Core mapping for the LearningCatalog entity.
    /// </summary>
    /// <param name="builder">The <see cref="EntityTypeBuilder{LearningCatalog}"/> used to configure keys, properties, and relationships for the LearningCatalog entity.</param>
    /// <remarks>
    /// Sets the primary key to <c>Id</c>, requires <c>Name</c> with a maximum length of 200 characters, configures a one-to-many relationship to <c>Courses</c> with cascade delete, and configures a many-to-many relationship to <c>Tags</c> using the join table named "CatalogTopicTag". 
    /// </remarks>
    public void Configure(EntityTypeBuilder<LearningCatalog> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);

        // One-to-many (unsupported in v2 — present for FR-018/FR-019 testing)
        builder.HasMany(c => c.Courses)
            .WithOne(c => c.Catalog)
            .HasForeignKey(c => c.CatalogId)
            .OnDelete(DeleteBehavior.Cascade);

        // Pure many-to-many via skip navigation
        builder.HasMany(c => c.Tags)
            .WithMany()
            .UsingEntity("CatalogTopicTag");
    }
}
