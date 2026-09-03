using Core.Models.Referrals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.DBContext.Configuration.Referrals
{
    public class ReferralExperienceConfiguration : IEntityTypeConfiguration<ReferralExperience>
    {

        public void Configure(EntityTypeBuilder<ReferralExperience> builder)
        {
            builder.HasData(
              new ReferralExperience
              {
                  Id = 1,
                  Description = "Junior (6 months to 1 year)",
                  Active = true
              },
              new ReferralExperience
              {
                  Id = 2,
                  Description = "Mid - level (1 to 3 year)",
                  Active = true
              },
              new ReferralExperience
              {
                  Id = 3,
                  Description = "Senior (3 years +)",
                  Active = true
              }
            );
        }
    }
}