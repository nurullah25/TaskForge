using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskForge.Api.Entities;

namespace TaskForge.Api.Data.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.Property(o => o.Name).HasMaxLength(100);
    }
}

public class OrganizationMemberConfiguration : IEntityTypeConfiguration<OrganizationMember>
{
    public void Configure(EntityTypeBuilder<OrganizationMember> builder)
    {
        // Composite key: a user can only be in an organization once.
        // EF adds a separate index on UserId for "my organizations" lookups.
        builder.HasKey(m => new { m.OrganizationId, m.UserId });

        builder.HasOne(m => m.Organization)
            .WithMany(o => o.Members)
            .HasForeignKey(m => m.OrganizationId);

        builder.HasOne(m => m.User)
            .WithMany(u => u.Organizations)
            .HasForeignKey(m => m.UserId);
    }
}
