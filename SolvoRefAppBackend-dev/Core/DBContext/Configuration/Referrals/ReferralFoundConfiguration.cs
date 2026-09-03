using Core.Models.Referrals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.DBContext.Configuration.Referrals
{
    public class ReferralFoundConfiguration : IEntityTypeConfiguration<ReferralFound>
    {

        public void Configure(EntityTypeBuilder<ReferralFound> builder)
        {
            builder.HasData(
              new ReferralFound
              {
                  Id = 1,
                  Description = "Email campaign",
                  Active = true
              },
              new ReferralFound
              {
                  Id = 2,
                  Description = "Social media ads",
                  Active = true
              },
              new ReferralFound
              {
                  Id = 3,
                  Description = "Program info banner",
                  Active = true
              },
              new ReferralFound
              {
                  Id = 4,
                  Description = "On-site activation or event",
                  Active = true
              },
              new ReferralFound
              {
                  Id = 5,
                  Description = "Corporate onboarding",
                  Active = true
              },
              new ReferralFound
              {
                  Id = 6,
                  Description = "Other",
                  Active = true
              }
            );
        }
    }
}