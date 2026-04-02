using Diwink.Extensions.EntityFrameworkCore.V2.TestModel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diwink.Extensions.EntityFrameworkCore.V2.TestModel.Configurations;

public class CourseMentorAssignmentConfiguration : IEntityTypeConfiguration<CourseMentorAssignment>
{
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
