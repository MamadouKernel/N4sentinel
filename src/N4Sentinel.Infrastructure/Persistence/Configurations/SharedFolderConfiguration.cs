using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class SharedFolderConfiguration : IEntityTypeConfiguration<SharedFolder>
{
    public void Configure(EntityTypeBuilder<SharedFolder> builder)
    {
        builder.ToTable("SharedFolders");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.Name).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Path).HasMaxLength(500).IsRequired();
        builder.Property(f => f.Category).HasConversion<string>().HasMaxLength(30);
        builder.Property(f => f.CorruptionStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.AnomalyDescription).HasMaxLength(1000);

        builder.HasIndex(f => new { f.EnvironmentId, f.Name }).IsUnique();
    }
}
