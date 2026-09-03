using Core.Models.Referrals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.DBContext.Configuration.Referrals
{
    public class ReferralConfiguration : IEntityTypeConfiguration<Referral>
    {
        public void Configure(EntityTypeBuilder<Referral> builder)
        {
            builder.HasKey(r => r.Id);

            builder
                .HasOne(r => r.Referrer)
                .WithMany()
                .HasForeignKey(r => r.ReferrerID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}