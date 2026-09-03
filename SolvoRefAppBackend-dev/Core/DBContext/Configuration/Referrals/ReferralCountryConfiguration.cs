using Core.Models.Referrals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.DBContext.Configuration.Referrals
{
    public class ReferralCountryConfiguration : IEntityTypeConfiguration<ReferralCountry>
    {

        public void Configure(EntityTypeBuilder<ReferralCountry> builder)
        {
            builder.HasData(
              new ReferralCountry
              {
                  Id = 1,
                  Description = "Colombia",
                  PhoneCode = "+57",
                  Active = true
              },
              new ReferralCountry
              {
                  Id = 2,
                  Description = "Argentina",
                  PhoneCode = "+54",
                  Active = true
              },
              new ReferralCountry
              {
                  Id = 3,
                  Description = "Mexico",
                  PhoneCode = "+52",
                  Active = true
              },
              new ReferralCountry
              {
                  Id = 4,
                  Description = "Guatemala",
                  PhoneCode = "+502",
                  Active = true
              },
              new ReferralCountry
              {
                  Id = 5,
                  Description = "Honduras",
                  PhoneCode = "+504",
                  Active = true
              },
              new ReferralCountry
              {
                  Id = 6,
                  Description = "Perú",
                  PhoneCode = "+51",
                  Active = true
              },
              new ReferralCountry
              {
                  Id = 7,
                  Description = "Kenya",
                  PhoneCode = "+254",
                  Active = true
              }
            );
        }
    }
}