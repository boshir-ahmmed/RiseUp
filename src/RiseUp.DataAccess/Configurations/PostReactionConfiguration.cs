using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Social;

namespace RiseUp.DataAccess.Configurations;

public sealed class PostReactionConfiguration : IEntityTypeConfiguration<PostReaction>
{
    public void Configure(EntityTypeBuilder<PostReaction> b)
    {
        b.HasIndex(x => new { x.PostId, x.ActorAccountId }).IsUnique();   // one like per account
        b.HasOne<StartupPost>().WithMany().HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
    }
}
