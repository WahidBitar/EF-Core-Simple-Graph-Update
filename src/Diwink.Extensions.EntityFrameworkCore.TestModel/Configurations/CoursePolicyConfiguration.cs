using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diwink.Extensions.EntityFrameworkCore.TestModel.Configurations;

public class CoursePolicyConfiguration : IEntityTypeConfiguration<CoursePolicy>
{
    /// <summary>
    /// Configures the CoursePolicy entity model: sets CourseId as the primary key and makes PolicyVersion required with a maximum length of 50 characters.
    /// </summary>
    /// <param name="builder">The EntityTypeBuilder for configuring the CoursePolicy entity.</param>
    public void Configure(EntityTypeBuilder<CoursePolicy> builder)
    {
        builder.HasKey(p => p.CourseId);
        builder.Property(p => p.PolicyVersion).IsRequired().HasMaxLength(50);
    }
}
