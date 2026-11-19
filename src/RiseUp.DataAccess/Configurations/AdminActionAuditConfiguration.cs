using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Governance;

namespace RiseUp.DataAccess.Configurations;

public sealed class AdminActionAuditConfiguration : IEntityTypeConfiguration<AdminActionAudit>
{
    public void Configure(EntityTypeBuilder<AdminActionAudit> b)
    {
        b.Property(x => x.ActorRole).HasMaxLength(60).IsRequired();
        b.Property(x => x.Action).HasMaxLength(200).IsRequired();
        b.Property(x => x.Target).HasMaxLength(200).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        b.Property(x => x.CorrelationId).HasMaxLength(64);
        b.HasIndex(x => x.CreatedAtUtc);
    }
}
