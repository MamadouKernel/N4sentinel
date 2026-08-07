using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class SopConfiguration : IEntityTypeConfiguration<Sop>
{
    public void Configure(EntityTypeBuilder<Sop> builder)
    {
        builder.ToTable("Sops");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.SopKey).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Title).HasMaxLength(300).IsRequired();
        builder.Property(s => s.Objective).IsRequired();
        builder.Property(s => s.StepsText).IsRequired();
        builder.Property(s => s.N4Version).HasMaxLength(50);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(s => new { s.SopKey, s.VersionNumber }).IsUnique();
    }
}
