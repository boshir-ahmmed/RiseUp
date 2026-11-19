using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Verification;

namespace RiseUp.DataAccess.Configurations;

public sealed class VerificationDocumentConfiguration : IEntityTypeConfiguration<VerificationDocument>
{
    public void Configure(EntityTypeBuilder<VerificationDocument> b)
    {
        b.Property(x => x.Kind).HasMaxLength(40).IsRequired();
        b.Property(x => x.StorageKey).HasMaxLength(200).IsRequired();
    }
}
