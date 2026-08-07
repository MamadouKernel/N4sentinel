using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class DependentSystemConfiguration : IEntityTypeConfiguration<DependentSystem>
{
    public void Configure(EntityTypeBuilder<DependentSystem> builder)
    {
        builder.ToTable("DependentSystems");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(1000);
        builder.Property(s => s.Governance).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(s => new { s.EnvironmentId, s.Name }).IsUnique();
    }
}
