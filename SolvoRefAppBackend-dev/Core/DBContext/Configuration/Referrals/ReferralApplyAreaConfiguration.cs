using Core.Models.Referrals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.DBContext.Configuration.Referrals
{
    public class ReferralApplyAreaConfiguration : IEntityTypeConfiguration<ReferralApplyArea>
    {

        public void Configure(EntityTypeBuilder<ReferralApplyArea> builder)
        {
            builder.HasData(
              new ReferralApplyArea
              {
                  Id = 1,
                  Description = "Accounting & Financial",
                  Active = true
              },
              new ReferralApplyArea
              {
                  Id = 2,
                  Description = "Administrative",
                  Active = true
              },
              new ReferralApplyArea
              {
                  Id = 3,
                  Description = "Collections",
                  Active = true
              },
              new ReferralApplyArea
              {
                  Id = 4,
                  Description = "Customer service",
                  Active = true
              }, new ReferralApplyArea
              {
                  Id = 5,
                  Description = "IT",
                  Active = true
              },
              new ReferralApplyArea
              {
                  Id = 6,
                  Description = "Legal",
                  Active = true
              },
              new ReferralApplyArea
              {
                  Id = 7,
                  Description = "Marketing",
                  Active = true
              },
              new ReferralApplyArea
              {
                  Id = 8,
                  Description = "Recruitment",
                  Active = true
              },
              new ReferralApplyArea
              {
                  Id = 9,
                  Description = "Sales",
                  Active = true
              }
            );
        }
    }
}