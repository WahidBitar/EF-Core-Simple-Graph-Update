using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diwink.Extensions.EntityFrameworkCore.TestModel.Configurations;

public class MentorConfiguration : IEntityTypeConfiguration<Mentor>
{
    /// <summary>
    /// Configures the EF Core mapping for the Mentor entity.
    /// </summary>
    /// <remarks>
    /// Sets the primary key, requires DisplayName (max length 200) and Status (max length 50),
    /// and configures an optional one-to-one relationship to MentorWorkspace using MentorWorkspace.MentorId
    /// with delete behavior set to SetNull.
    /// </remarks>
    /// <param name="builder">The <see cref="EntityTypeBuilder{Mentor}"/> used to configure the Mentor entity.</param>
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
