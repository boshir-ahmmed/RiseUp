using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Payments;

namespace RiseUp.DataAccess.Configurations;

public sealed class PaymentOrderConfiguration : IEntityTypeConfiguration<PaymentOrder>
{
    public void Configure(EntityTypeBuilder<PaymentOrder> b)
    {
        b.Property(x => x.OrderNumber).HasMaxLength(30).IsRequired();
        b.HasIndex(x => x.OrderNumber).IsUnique();
        b.Property(x => x.PriceBdtSnapshot).HasPrecision(18, 2);
        b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        b.Property(x => x.ProviderTransactionId).HasMaxLength(100);
        b.HasIndex(x => x.ProviderTransactionId).IsUnique().HasFilter("[ProviderTransactionId] IS NOT NULL");
        b.Property(x => x.Method).HasMaxLength(40);
    }
}
