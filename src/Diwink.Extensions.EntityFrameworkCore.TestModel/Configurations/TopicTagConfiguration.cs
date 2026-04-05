using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diwink.Extensions.EntityFrameworkCore.TestModel.Configurations;

public class TopicTagConfiguration : IEntityTypeConfiguration<TopicTag>
{
    /// <summary>
    /// Configures the EF Core mapping for the TopicTag entity.
    /// </summary>
    /// <param name="builder">The EntityTypeBuilder for TopicTag used to configure the primary key, property constraints, and indexes.</param>
    public void Configure(EntityTypeBuilder<TopicTag> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Label).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => t.Label).IsUnique();
    }
}
