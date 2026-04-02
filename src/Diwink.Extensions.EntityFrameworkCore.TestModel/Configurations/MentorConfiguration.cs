using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diwink.Extensions.EntityFrameworkCore.TestModel.Configurations;

public class MentorConfiguration : IEntityTypeConfiguration<Mentor>
{
    public void Configure(EntityTypeBuilder<Mentor> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Status).IsRequired().HasMaxLength(50);

        // Optional one-to-one: MentorWorkspace
        builder.HasOne(m => m.Workspace)
            .WithOne(w => w.Mentor)
            .HasForeignKey<MentorWorkspace>(w => w.MentorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
