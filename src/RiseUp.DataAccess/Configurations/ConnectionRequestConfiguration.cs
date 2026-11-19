using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Connections;
using RiseUp.Entities.Common;
using RiseUp.Entities.Startups;
using RiseUp.Entities.Investors;

namespace RiseUp.DataAccess.Configurations;

public sealed class ConnectionRequestConfiguration : IEntityTypeConfiguration<ConnectionRequest>
{
    public void Configure(EntityTypeBuilder<ConnectionRequest> b)
    {
        // NFR-10: one pending request per pair regardless of type or direction, enforced by the database.
        b.HasIndex(x => new { x.StartupId, x.InvestorId }).IsUnique().HasFilter("[Status] = 0").HasDatabaseName("UX_ConnectionRequest_PendingPair");
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.HasIndex(x => new { x.Status, x.ExpiresAtUtc });            // expiry job
        b.HasIndex(x => new { x.InvestorId, x.Type, x.SentAtUtc });
        b.Property(x => x.Message).HasMaxLength(3000).IsRequired();
        b.Property(x => x.DecisionNote).HasMaxLength(300);
        b.Property(x => x.AiDecisionVersion).HasMaxLength(40);
        b.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
        b.Property(x => x.AttachedDocumentIds).HasMaxLength(100).IsRequired();
        b.Property(x => x.InfoRequestMessage).HasMaxLength(500);
        b.Ignore(x => x.AttachedDocuments);
        b.HasOne<Startup>().WithMany().HasForeignKey(x => x.StartupId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<InvestorProfile>().WithMany().HasForeignKey(x => x.InvestorId).OnDelete(DeleteBehavior.Restrict);
    }
}
