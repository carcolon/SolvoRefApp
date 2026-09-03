using Core.Models.Configurations;
using Core.Models.Referrals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.DBContext.Configuration.Configurations
{
    public class CountryHuntyInformationConfiguration : IEntityTypeConfiguration<CountryHuntyInformation>
    {

        public void Configure(EntityTypeBuilder<CountryHuntyInformation> builder)
        {
            builder.Property(item => item.ProgramName).HasMaxLength(150);
            builder.Property(item => item.ProgramType).HasMaxLength(50);

            builder.HasData(
                new CountryHuntyInformation
                {
                    Id = 1,
                    Country = "Colombia",
                    ProgramName = "Referral Program Colombia",
                    ProgramType = "Referidos",
                    VacancyId = "69fa4ef050598f0910812fbc",
                    CompanyId = "f5a1e34a-c171-4660-aef4-4d84d8e98c3c",
                    Api_key = "ak_REDACTED"
                },
              new CountryHuntyInformation
              {
                  Id = 2,
                  Country = "Argentina",
                  ProgramName = "Referall Program Argentina",
                  ProgramType = "Referidos",
                  VacancyId = "69fcf140fd194c2d84863ad4",
                  CompanyId = "92ecedb8-d6ab-477a-84e9-03663e2d80b1",
                  Api_key = "ak_REDACTED"
              },
              new CountryHuntyInformation
              {
                  Id = 3,
                  Country = "Mexico",
                  ProgramName = "Referral Program Mexico",
                  ProgramType = "Referidos",
                  VacancyId = "69fe5f823054dc6c56a46ffe",
                  CompanyId = "e8d866b1-1743-4504-92f2-8ef0a64d74ec",
                  Api_key = "ak_REDACTED"
              },
              new CountryHuntyInformation
              {
                  Id = 4,
                  Country = "Guatemala",
                  ProgramName = "Referral Program Guatemala",
                  ProgramType = "Referidos",
                  VacancyId = "69fe6c753054dc6c56a48257",
                  CompanyId = "defa286f-c14a-453d-8500-b24ed080ed82",
                  Api_key = "ak_REDACTED"
              },
              new CountryHuntyInformation
              {
                  Id = 5,
                  Country = "Honduras",
                  ProgramName = "Referall Program Honduras",
                  ProgramType = "Referidos",
                  VacancyId = "69fe01153054dc6c56a3eee4",
                  CompanyId = "8ac2eaa9-da7c-4a4b-8ed1-bb43c264c95e",
                  Api_key = "ak_REDACTED"
              },
              new CountryHuntyInformation
              {
                  Id = 6,
                  Country = "Peru",
                  ProgramName = "Referral Program Peru",
                  ProgramType = "Referidos",
                  VacancyId = "69fe0639df45ad5a5d249634",
                  CompanyId = "d3f8fbdc-08fc-462d-bdb7-35e730032fda",
                  Api_key = "ak_REDACTED"
              },
              new CountryHuntyInformation
              {
                  Id = 7,
                  Country = "Kenya",
                  ProgramName = "Referall Program Kenya",
                  ProgramType = "Referidos",
                  VacancyId = "69fe687850598f09108203b9",
                  CompanyId = "1a20f16e-08d2-46cc-90b8-00b4104d936a",
                  Api_key = "ak_REDACTED"
              },
              new CountryHuntyInformation
              {
                  Id = 8,
                  Country = "Argentina",
                  ProgramName = "Vacantes TBPO Argentina Referidos",
                  ProgramType = "Referidos",
                  VacancyId = "6a5962062071b02aca17aaa0",
                  CompanyId = "92ecedb8-d6ab-477a-84e9-03663e2d80b1",
                  Api_key = "ak_REDACTED"
              },
              new CountryHuntyInformation
              {
                  Id = 9,
                  Country = "Colombia",
                  ProgramName = "Vacantes TBPO Colombia Referidos",
                  ProgramType = "Referidos",
                  VacancyId = "6a5965ed564d697f4d61f520",
                  CompanyId = "f5a1e34a-c171-4660-aef4-4d84d8e98c3c",
                  Api_key = "ak_REDACTED"
              },
              new CountryHuntyInformation
              {
                  Id = 10,
                  Country = "Mexico",
                  ProgramName = "Vacantes TBPO Mexico Referidos",
                  ProgramType = "Referidos",
                  VacancyId = "6a5a2e98b3c00994dc176018",
                  CompanyId = "e8d866b1-1743-4504-92f2-8ef0a64d74ec",
                  Api_key = "ak_REDACTED"
              }
            );
        }
    }
}
