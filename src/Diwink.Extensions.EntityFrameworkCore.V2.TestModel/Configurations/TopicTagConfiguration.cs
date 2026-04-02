using Diwink.Extensions.EntityFrameworkCore.V2.TestModel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diwink.Extensions.EntityFrameworkCore.V2.TestModel.Configurations;

public class TopicTagConfiguration : IEntityTypeConfiguration<TopicTag>
{
    public void Configure(EntityTypeBuilder<TopicTag> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Label).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => t.Label).IsUnique();
    }
}
