using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diwink.Extensions.EntityFrameworkCore.TestModel.Configurations;

public class CoursePolicyConfiguration : IEntityTypeConfiguration<CoursePolicy>
{
    public void Configure(EntityTypeBuilder<CoursePolicy> builder)
    {
        builder.HasKey(p => p.CourseId);
        builder.Property(p => p.PolicyVersion).IsRequired().HasMaxLength(50);
    }
}
