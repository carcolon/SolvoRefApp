using Core.Models.Referrals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.DBContext.Configuration.Referrals
{
    public class ReferralAccountConfiguration : IEntityTypeConfiguration<ReferralAccount>
    {

        public void Configure(EntityTypeBuilder<ReferralAccount> builder)
        {
            builder.HasData(
              new ReferralAccount
              {
                  Id = 1,
                  Description = "Vensure",
                  Active = true
              },
              new ReferralAccount
              {
                  Id = 2,
                  Description = "Staff",
                  Active = true
              },
              new ReferralAccount
              {
                  Id = 3,
                  Description = "Inktel",
                  Active = true
              },
              new ReferralAccount
              {
                  Id = 4,
                  Description = "TBPO CR and SALES roles (Cyracom, Uly, TPG - Travel Pass, Propio, Nolan, Netsol, JLR, Truly, Urgently EHI UDA, Spirit, Honk, TTC)",
                  Active = true
              },
              new ReferralAccount
              {
                  Id = 5,
                  Description = "Pink Program (isolved)",
                  Active = true
              },
              new ReferralAccount
              {
                  Id = 6,
                  Description = "BackOffice",
                  Active = true
              },
              new ReferralAccount
              {
                  Id = 7,
                  Description = "I'm not referring to any particular account",
                  Active = true
              },
              new ReferralAccount
              {
                  Id = 8,
                  Description = "Other",
                  Active = true
              }
            );
        }
    }
}
