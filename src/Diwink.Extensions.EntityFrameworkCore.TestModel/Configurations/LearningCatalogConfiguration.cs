using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diwink.Extensions.EntityFrameworkCore.TestModel.Configurations;

public class LearningCatalogConfiguration : IEntityTypeConfiguration<LearningCatalog>
{
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
