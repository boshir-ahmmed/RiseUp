using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Investors;
using RiseUp.Entities.Identity;

namespace RiseUp.DataAccess.Configurations;

public sealed class InvestorProfileConfiguration : IEntityTypeConfiguration<InvestorProfile>
{
    public void Configure(EntityTypeBuilder<InvestorProfile> b)
    {
        b.HasIndex(x => x.AccountId).IsUnique();
        b.HasIndex(x => new { x.VerificationStatus, x.IsSuspended, x.IsDeleted });
        b.Property(x => x.DisplayName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Affiliation).HasMaxLength(150);
        b.Property(x => x.InvestorType).HasMaxLength(60);
        b.Property(x => x.Bio).HasMaxLength(1000);
        b.Property(x => x.PreferredIndustries).HasMaxLength(400);
        b.Property(x => x.PreferredStages).HasMaxLength(100);
        b.Property(x => x.Location).HasMaxLength(80);
        b.Property(x => x.TicketMinUsd).HasPrecision(18, 2);
        b.Property(x => x.TicketMaxUsd).HasPrecision(18, 2);
        b.ToTable(t => t.HasCheckConstraint("CK_Investor_TicketRange", "[TicketMinUsd] <= [TicketMaxUsd]"));
        b.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Claims).WithOne().HasForeignKey(c => c.InvestorId).OnDelete(DeleteBehavior.Cascade);
    }
}
