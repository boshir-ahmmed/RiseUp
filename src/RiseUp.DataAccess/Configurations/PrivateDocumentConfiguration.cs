using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Startups;

namespace RiseUp.DataAccess.Configurations;

public sealed class PrivateDocumentConfiguration : IEntityTypeConfiguration<PrivateDocument>
{
    public void Configure(EntityTypeBuilder<PrivateDocument> b)
    {
        b.Property(x => x.FileName).HasMaxLength(200).IsRequired();
        b.Property(x => x.StorageKey).HasMaxLength(200).IsRequired();
        b.HasOne<Startup>().WithMany().HasForeignKey(x => x.StartupId).OnDelete(DeleteBehavior.Cascade);
    }
}
