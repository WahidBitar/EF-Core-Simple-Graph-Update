using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diwink.Extensions.EntityFrameworkCore.TestModel.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    /// <summary>
    /// Configures the EF Core model mapping for the <c>Course</c> entity.
    /// </summary>
    /// <param name="builder">The entity type builder for configuring <c>Course</c>.</param>
    /// <remarks>
    /// - Sets <c>Id</c> as the primary key.
    /// - Marks <c>Title</c> and <c>Code</c> as required with maximum lengths of 300 and 50 respectively.
    /// - Adds a unique composite index on <c>{ CatalogId, Code }</c>.
    /// - Configures a required one-to-one relationship to <c>CoursePolicy</c> with <c>CoursePolicy.CourseId</c> as the foreign key and cascade delete.
    /// - Configures a many-to-many relationship with <c>Tag</c> using the join table named <c>CourseTopicTag</c>.
    /// </remarks>
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
