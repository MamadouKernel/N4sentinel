using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.ActorUserId).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Action).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Summary).HasMaxLength(1000);

        builder.HasIndex(e => e.OccurredAtUtc);
    }
}
