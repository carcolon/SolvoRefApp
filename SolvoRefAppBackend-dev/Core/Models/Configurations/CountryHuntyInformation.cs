using System.ComponentModel.DataAnnotations;

namespace Core.Models.Configurations
{
    public class CountryHuntyInformation
    {
        [Key]
        public int Id { get; set; }
        public string Country { get; set; } = string.Empty;
        public string ProgramName { get; set; } = string.Empty;
        public string ProgramType { get; set; } = string.Empty;
        public string VacancyId { get; set; } = string.Empty;
        public string CompanyId { get; set; } = string.Empty;
        public string Api_key { get; set; } = string.Empty;
    }
}
