using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.DocumentKey).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Title).HasMaxLength(300).IsRequired();
        builder.Property(d => d.Category).HasConversion<string>().HasMaxLength(30);
        builder.Property(d => d.N4Version).HasMaxLength(50);
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(d => new { d.DocumentKey, d.VersionNumber }).IsUnique();
    }
}
