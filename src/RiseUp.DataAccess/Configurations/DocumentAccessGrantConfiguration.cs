using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Startups;
using RiseUp.Entities.Investors;

namespace RiseUp.DataAccess.Configurations;

public sealed class DocumentAccessGrantConfiguration : IEntityTypeConfiguration<DocumentAccessGrant>
{
    public void Configure(EntityTypeBuilder<DocumentAccessGrant> b)
    {
        b.HasIndex(x => new { x.DocumentId, x.InvestorId }).IsUnique().HasFilter("[RevokedAtUtc] IS NULL");
        b.HasOne<PrivateDocument>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<InvestorProfile>().WithMany().HasForeignKey(x => x.InvestorId).OnDelete(DeleteBehavior.Restrict);
    }
}
