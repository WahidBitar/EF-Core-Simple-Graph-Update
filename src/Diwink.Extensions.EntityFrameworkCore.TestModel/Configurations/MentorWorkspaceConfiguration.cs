using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diwink.Extensions.EntityFrameworkCore.TestModel.Configurations;

public class MentorWorkspaceConfiguration : IEntityTypeConfiguration<MentorWorkspace>
{
    /// <summary>
    /// Configures the Entity Framework Core mapping for the MentorWorkspace entity.
    /// </summary>
    /// <param name="builder">The EntityTypeBuilder for MentorWorkspace used to set the primary key and property constraints (DeskCode and Building).</param>
    public void Configure(EntityTypeBuilder<MentorWorkspace> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.DeskCode).IsRequired().HasMaxLength(20);
        builder.Property(w => w.Building).IsRequired().HasMaxLength(100);
    }
}
