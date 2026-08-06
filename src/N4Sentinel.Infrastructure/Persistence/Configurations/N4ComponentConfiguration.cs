using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class N4ComponentConfiguration : IEntityTypeConfiguration<N4Component>
{
    public void Configure(EntityTypeBuilder<N4Component> builder)
    {
        builder.ToTable("Components");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Role).HasMaxLength(200).IsRequired();
        builder.Property(c => c.HostName).HasMaxLength(255);
        builder.Property(c => c.IpAddress).HasMaxLength(45);
        builder.Property(c => c.DnsName).HasMaxLength(255);
        builder.Property(c => c.OperatingSystem).HasMaxLength(100);
        builder.Property(c => c.ServiceOrProcessName).HasMaxLength(255);
        builder.Property(c => c.HealthCheckDescription).HasMaxLength(1000);
        builder.Property(c => c.TechnicalOwner).HasMaxLength(200);
        builder.Property(c => c.FunctionalOwner).HasMaxLength(200);
        builder.Property(c => c.Criticality).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Governance).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(c => new { c.EnvironmentId, c.Name }).IsUnique();

        // Collection scalaire (liste d'ids) : stockée en colonne JSON native EF Core 8+ ("primitive collection").
        builder.Property(c => c.DependsOnComponentIds)
            .HasField("_dependsOnComponentIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("DependsOnComponentIds");
    }
}
