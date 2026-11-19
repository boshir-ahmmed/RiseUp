using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Payments;

namespace RiseUp.DataAccess.Configurations;

public sealed class PaymentProviderEventConfiguration : IEntityTypeConfiguration<PaymentProviderEvent>
{
    public void Configure(EntityTypeBuilder<PaymentProviderEvent> b)
    {
        b.Property(x => x.ProviderTransactionId).HasMaxLength(100).IsRequired();
        b.Property(x => x.PayloadHash).HasMaxLength(128).IsRequired();
        b.Property(x => x.Result).HasMaxLength(200).IsRequired();
    }
}
