using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Startups;

namespace RiseUp.DataAccess.Configurations;

public sealed class TeamAccountConfiguration : IEntityTypeConfiguration<TeamAccount>
{
    public void Configure(EntityTypeBuilder<TeamAccount> b)
    {
        b.Property(x => x.Username).HasMaxLength(30).IsRequired();
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.HasIndex(x => x.Username).IsUnique();
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Designation).HasMaxLength(100).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        b.Property(x => x.PreviousConversationIds).HasMaxLength(500);
        b.HasIndex(x => new { x.StartupId, x.Status });
        b.HasOne<Startup>().WithMany().HasForeignKey(x => x.StartupId).OnDelete(DeleteBehavior.Cascade);
    }
}
