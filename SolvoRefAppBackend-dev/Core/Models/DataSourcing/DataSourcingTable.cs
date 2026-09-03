using System.ComponentModel.DataAnnotations;

namespace Core.Models.DataSourcing
{
    public class DataSourcingTable
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ApplyArea { get; set; } = string.Empty;
        public string DNI { get; set; } = string.Empty;
        public string Experience { get; set; } = string.Empty;
        public string EnglishLevel { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string? City { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public string AdSetName { get; set; } = "Referidos";
        public string Company { get; set; } = "Referidos";
        public string Position { get; set; } = string.Empty;
        public string VacancyId { get; set; } = string.Empty;
        public string ExternalVacancyId { get; set; } = string.Empty;
        public string CompanyId { get; set; } = string.Empty;
        public string Api_key { get; set; } = string.Empty;
        public string ReferrerEmployeeId { get; set; } = string.Empty;
        public string ReferrerSolvoPartnerStatus { get; set; } = "Inactive";
        public string ReferralFromSolvoPartner { get; set; } = "No";
    }
}
