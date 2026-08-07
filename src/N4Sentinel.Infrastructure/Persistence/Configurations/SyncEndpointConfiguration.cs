using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class SyncEndpointConfiguration : IEntityTypeConfiguration<SyncEndpoint>
{
    public void Configure(EntityTypeBuilder<SyncEndpoint> builder)
    {
        builder.ToTable("SyncEndpoints");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.AnomalyDescription).HasMaxLength(1000);

        builder.HasIndex(e => new { e.EnvironmentId, e.Name }).IsUnique();
    }
}
