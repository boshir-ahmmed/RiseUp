using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Investors;

namespace RiseUp.DataAccess.Configurations;

public sealed class InvestorClaimConfiguration : IEntityTypeConfiguration<InvestorClaim>
{
    public void Configure(EntityTypeBuilder<InvestorClaim> b)
    {
        b.Property(x => x.StartupName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Round).HasMaxLength(40).IsRequired();
        b.Property(x => x.EvidenceReference).HasMaxLength(200);
    }
}
