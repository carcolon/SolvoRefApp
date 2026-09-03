using Core.Models.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.DBContext.Configuration.Configurations
{
    public class HolyDatesCountryCodeConfiguration : IEntityTypeConfiguration<HolyDatesCountryCode>
    {
        public void Configure(EntityTypeBuilder<HolyDatesCountryCode> builder)
        {
            builder.HasData
            (
                new HolyDatesCountryCode { Id = 1, DataLakeCountryName = "kenya", NagerCountryCode = "KE  " },
                new HolyDatesCountryCode { Id = 2, DataLakeCountryName = "guatemala", NagerCountryCode = "GT" },
                new HolyDatesCountryCode { Id = 3, DataLakeCountryName = "el salvador", NagerCountryCode = "SV" },
                new HolyDatesCountryCode { Id = 4, DataLakeCountryName = "nicaragua", NagerCountryCode = "NI" },
                new HolyDatesCountryCode { Id = 5, DataLakeCountryName = "honduras", NagerCountryCode = "HN" },
                new HolyDatesCountryCode { Id = 6, DataLakeCountryName = "belize", NagerCountryCode = "BZ" },
                new HolyDatesCountryCode { Id = 7, DataLakeCountryName = "philippines", NagerCountryCode = "PH" },
                new HolyDatesCountryCode { Id = 8, DataLakeCountryName = "jamaica", NagerCountryCode = "JM" },
                new HolyDatesCountryCode { Id = 9, DataLakeCountryName = "argentina", NagerCountryCode = "AR" },
                new HolyDatesCountryCode { Id = 10, DataLakeCountryName = "brasil", NagerCountryCode = "BR" },
                new HolyDatesCountryCode { Id = 11, DataLakeCountryName = "chile", NagerCountryCode = "CL" },
                new HolyDatesCountryCode { Id = 12, DataLakeCountryName = "perú", NagerCountryCode = "PE" },
                new HolyDatesCountryCode { Id = 13, DataLakeCountryName = "españa", NagerCountryCode = "ES" },
                new HolyDatesCountryCode { Id = 14, DataLakeCountryName = "india", NagerCountryCode = "IN" },
                new HolyDatesCountryCode { Id = 15, DataLakeCountryName = "united states", NagerCountryCode = "US" },
                new HolyDatesCountryCode { Id = 16, DataLakeCountryName = "ecuador", NagerCountryCode = "EC" },
                new HolyDatesCountryCode { Id = 17, DataLakeCountryName = "nigeria", NagerCountryCode = "NG" },
                new HolyDatesCountryCode { Id = 18, DataLakeCountryName = "dominican republic", NagerCountryCode = "DO" },
                new HolyDatesCountryCode { Id = 19, DataLakeCountryName = "greece", NagerCountryCode = "GR" },
                new HolyDatesCountryCode { Id = 20, DataLakeCountryName = "costa rica", NagerCountryCode = "CR" },
                new HolyDatesCountryCode { Id = 21, DataLakeCountryName = "paraguay", NagerCountryCode = "PY" },
                new HolyDatesCountryCode { Id = 22, DataLakeCountryName = "canada", NagerCountryCode = "CA" },
                new HolyDatesCountryCode { Id = 23, DataLakeCountryName = "south africa", NagerCountryCode = "ZA" }
            );
        }
    }
}