using Diwink.Extensions.EntityFrameworkCore.V2.TestModel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diwink.Extensions.EntityFrameworkCore.V2.TestModel.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Title).IsRequired().HasMaxLength(300);
        builder.Property(c => c.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(c => new { c.CatalogId, c.Code }).IsUnique();

        // Required one-to-one: CoursePolicy
        builder.HasOne(c => c.Policy)
            .WithOne(p => p.Course)
            .HasForeignKey<CoursePolicy>(p => p.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Pure many-to-many via skip navigation
        builder.HasMany(c => c.Tags)
            .WithMany(t => t.Courses)
            .UsingEntity("CourseTopicTag");
    }
}
