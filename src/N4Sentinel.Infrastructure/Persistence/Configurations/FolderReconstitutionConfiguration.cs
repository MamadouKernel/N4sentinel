using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class FolderReconstitutionConfiguration : IEntityTypeConfiguration<FolderReconstitution>
{
    public void Configure(EntityTypeBuilder<FolderReconstitution> builder)
    {
        builder.ToTable("FolderReconstitutions");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(r => r.StartedByUserId).HasMaxLength(256).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.AbortReason).HasMaxLength(2000);

        builder.HasIndex(r => r.SharedFolderId);

        builder.HasMany(r => r.Steps)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(FolderReconstitution.Steps))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
