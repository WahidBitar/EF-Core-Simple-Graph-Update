using Diwink.Extensions.EntityFrameworkCore.V2.TestModel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diwink.Extensions.EntityFrameworkCore.V2.TestModel.Configurations;

public class MentorWorkspaceConfiguration : IEntityTypeConfiguration<MentorWorkspace>
{
    public void Configure(EntityTypeBuilder<MentorWorkspace> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.DeskCode).IsRequired().HasMaxLength(20);
        builder.Property(w => w.Building).IsRequired().HasMaxLength(100);
    }
}
