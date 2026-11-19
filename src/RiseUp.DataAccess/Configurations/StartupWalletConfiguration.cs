using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Wallets;
using RiseUp.Entities.Startups;

namespace RiseUp.DataAccess.Configurations;

public sealed class StartupWalletConfiguration : IEntityTypeConfiguration<StartupWallet>
{
    public void Configure(EntityTypeBuilder<StartupWallet> b)
    {
        b.HasIndex(x => x.StartupId).IsUnique();
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Wallet_Welcome_NonNegative", "[WelcomeBalance] >= 0");
            t.HasCheckConstraint("CK_Wallet_Purchased_NonNegative", "[PurchasedBalance] >= 0");
        });
        b.HasOne<Startup>().WithMany().HasForeignKey(x => x.StartupId).OnDelete(DeleteBehavior.Restrict);
    }
}
