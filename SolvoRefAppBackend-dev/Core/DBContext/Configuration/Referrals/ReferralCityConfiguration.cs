using Core.Models.Referrals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.DBContext.Configuration.Referrals
{
    public class ReferralCityConfiguration : IEntityTypeConfiguration<ReferralCity>
    {

        public void Configure(EntityTypeBuilder<ReferralCity> builder)
        {
            builder.HasData(
              new ReferralCity
              {
                  Id = 1,
                  Description = "Medellín",
                  CountryId = 1,
                  Active = true
              },
               new ReferralCity
               {
                   Id = 2,
                   Description = "Bogotá",
                   CountryId = 1,
                   Active = true
               },
              new ReferralCity
              {
                  Id = 3,
                  Description = "Barranquilla",
                  CountryId = 1,
                  Active = true
              },
               new ReferralCity
               {
                   Id = 4,
                   Description = "Cali",
                   CountryId = 1,
                   Active = true
               },
               new ReferralCity
               {
                   Id = 5,
                   Description = "Bucaramanga",
                   CountryId = 1,
                   Active = true
               },
               new ReferralCity
               {
                   Id = 6,
                   Description = "Buenos Aires -CABA",
                   CountryId = 2,
                   Active = true
               },
               new ReferralCity
               {
                   Id = 7,
                   Description = "Mendoza",
                   CountryId = 2,
                   Active = true
               },
               new ReferralCity
               {
                   Id = 8,
                   Description = "Córdoba",
                   CountryId = 2,
                   Active = true
               },
               new ReferralCity
               {
                   Id = 9,
                   Description = "Mérida",
                   CountryId = 3,
                   Active = true
               },
               new ReferralCity
               {
                   Id = 10,
                   Description = "Chihuahua",
                   CountryId = 3,
                   Active = true
               },
               new ReferralCity
               {
                   Id = 11,
                   Description = "Ciudad de Guatemala",
                   CountryId = 4,
                   Active = true
               },
               new ReferralCity
               {
                   Id = 12,
                   Description = "Tegucugalpa",
                   CountryId = 5,
                   Active = true
               },
               new ReferralCity
               {
                   Id = 13,
                   Description = "San Pedro de Sula",
                   CountryId = 5,
                   Active = true
               },
               new ReferralCity
               {
                   Id = 14,
                   Description = "Lima",
                   CountryId = 6,
                   Active = true
               }
            );
        }
    }
}