using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Social;

namespace RiseUp.DataAccess.Configurations;

public sealed class PostCommentConfiguration : IEntityTypeConfiguration<PostComment>
{
    public void Configure(EntityTypeBuilder<PostComment> b)
    {
        b.Property(x => x.Body).HasMaxLength(1000).IsRequired();
        b.Property(x => x.AuthorName).HasMaxLength(150).IsRequired();
        b.ToTable(t => t.HasCheckConstraint("CK_Comment_Depth", "[Depth] BETWEEN 1 AND 3"));
        b.HasIndex(x => new { x.PostId, x.CreatedAtUtc });
        b.HasOne<StartupPost>().WithMany().HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<PostComment>().WithMany().HasForeignKey(x => x.ParentCommentId).OnDelete(DeleteBehavior.NoAction);
    }
}
