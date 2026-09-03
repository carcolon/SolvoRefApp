using Core.Models.Referrals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.DBContext.Configuration.Referrals
{
    public class ReferralLinkConfiguration : IEntityTypeConfiguration<ReferralLink>
    {
        public void Configure(EntityTypeBuilder<ReferralLink> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Token)
                .HasMaxLength(128)
                .IsRequired();

            builder.HasIndex(x => x.Token)
                .IsUnique();

            builder.HasIndex(x => x.ReferrerId);

            builder.HasOne(x => x.Referrer)
                .WithMany()
                .HasForeignKey(x => x.ReferrerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
