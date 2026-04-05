using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diwink.Extensions.EntityFrameworkCore.TestModel.Configurations;

public class CourseMentorAssignmentConfiguration : IEntityTypeConfiguration<CourseMentorAssignment>
{
    /// <summary>
    /// Configures the EF Core model for the CourseMentorAssignment entity.
    /// </summary>
    /// <remarks>
    /// Defines a composite primary key (CourseId, MentorId), configures property constraints for Role, AssignedOnUtc and AllocationPercent,
    /// and establishes required relationships to Course and Mentor with cascade delete behavior.
    /// </remarks>
    /// <param name="builder">The EntityTypeBuilder for CourseMentorAssignment used to apply the configuration.</param>
    public void Configure(EntityTypeBuilder<CourseMentorAssignment> builder)
    {
        builder.HasKey(a => new { a.CourseId, a.MentorId });

        builder.Property(a => a.Role).IsRequired().HasMaxLength(100);
        builder.Property(a => a.AssignedOnUtc).IsRequired();
        builder.Property(a => a.AllocationPercent).HasPrecision(5, 2);

        builder.HasOne(a => a.Course)
            .WithMany(c => c.MentorAssignments)
            .HasForeignKey(a => a.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Mentor)
            .WithMany(m => m.CourseAssignments)
            .HasForeignKey(a => a.MentorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
