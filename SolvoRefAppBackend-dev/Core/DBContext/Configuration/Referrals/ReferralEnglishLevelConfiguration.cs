using Core.Models.Referrals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.DBContext.Configuration.Referrals
{
    public class ReferralEnglishLevelConfiguration : IEntityTypeConfiguration<ReferralEnglishLevel>
    {

        public void Configure(EntityTypeBuilder<ReferralEnglishLevel> builder)
        {
            builder.HasData(
              new ReferralEnglishLevel
              {
                  Id = 1,
                  Description = "B2-Intermediate",
                  Active = true
              },
              new ReferralEnglishLevel
              {
                  Id = 2,
                  Description = "C1-Advanced",
                  Active = true
              },
              new ReferralEnglishLevel
              {
                  Id = 3,
                  Description = "C2-Professional",
                  Active = true
              }
            );
        }
    }
}