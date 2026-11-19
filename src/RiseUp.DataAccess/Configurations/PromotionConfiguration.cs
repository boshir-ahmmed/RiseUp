using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Promotions;

namespace RiseUp.DataAccess.Configurations;

public sealed class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> b)
    {
        b.Property(x => x.PauseReason).HasMaxLength(200);
        b.HasIndex(x => new { x.Status, x.StartupId });
        b.HasIndex(x => x.PostId).IsUnique().HasFilter("[Status] <> 3");   // one open promotion per post
    }
}
