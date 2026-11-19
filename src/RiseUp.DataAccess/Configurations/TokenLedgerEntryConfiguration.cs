using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Wallets;

namespace RiseUp.DataAccess.Configurations;

public sealed class TokenLedgerEntryConfiguration : IEntityTypeConfiguration<TokenLedgerEntry>
{
    public void Configure(EntityTypeBuilder<TokenLedgerEntry> b)
    {
        b.Property(x => x.Description).HasMaxLength(200).IsRequired();
        b.Property(x => x.Reference).HasMaxLength(100).IsRequired();
        b.Property(x => x.IdempotencyKey).HasMaxLength(120).IsRequired();
        b.HasIndex(x => x.IdempotencyKey).IsUnique();              // e.g. "welcome:{startupId}" can only post once
        b.HasIndex(x => new { x.StartupId, x.CreatedAtUtc });
    }
}
