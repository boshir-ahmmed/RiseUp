using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Identity;

namespace RiseUp.DataAccess.Configurations;

public sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> b)
    {
        b.ToTable(t => t.HasTrigger("TR_UserAccounts_Contacts"));
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.NormalizedEmail).HasMaxLength(256).IsRequired();
        b.HasIndex(x => x.NormalizedEmail).IsUnique();
        b.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(30);
        b.Property(x => x.MfaSecret).HasMaxLength(128);
        b.Property(x => x.AcceptedTermsVersion).HasMaxLength(16);
        b.Property(x => x.AcceptedPrivacyVersion).HasMaxLength(16);
    }
}
