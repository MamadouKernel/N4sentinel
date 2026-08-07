using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Configurations;

public class UserEnvironmentRoleConfiguration : IEntityTypeConfiguration<UserEnvironmentRole>
{
    public void Configure(EntityTypeBuilder<UserEnvironmentRole> builder)
    {
        builder.ToTable("UserEnvironmentRoles");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.UserId).HasMaxLength(450).IsRequired();
        builder.Property(r => r.Role).HasMaxLength(50).IsRequired();
        builder.Property(r => r.GrantedByUserId).HasMaxLength(450).IsRequired();

        builder.HasIndex(r => new { r.UserId, r.EnvironmentId });
        builder.HasIndex(r => r.EnvironmentId);
    }
}
