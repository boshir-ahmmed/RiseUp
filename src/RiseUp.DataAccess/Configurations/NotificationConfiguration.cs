using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Governance;

namespace RiseUp.DataAccess.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.Property(x => x.Text).HasMaxLength(300).IsRequired();
        b.Property(x => x.Link).HasMaxLength(200);
        b.HasIndex(x => new { x.RecipientAccountId, x.IsRead });
    }
}
