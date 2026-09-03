using System.ComponentModel.DataAnnotations;

namespace Core.Models.Configurations
{
    public class HolyDatesCountryCode
    {
        [Key]
        public int Id { get; set; }
        public string DataLakeCountryName { get; set; } = string.Empty;
        public string NagerCountryCode { get; set; } = string.Empty;
    }
}