using System.ComponentModel.DataAnnotations;
using Core.Security;
using Microsoft.AspNetCore.Http;

namespace Core.Feature.Referrals.CreateReferral
{
    public class CreateReferralDto
    {
        [Required]
        [StringLength(80, ErrorMessage = "FirstName cannot be longer than 80 characters.")]
        public string FirstName { get; set; } = string.Empty;
        [Required]
        [StringLength(80, ErrorMessage = "LastName cannot be longer than 80 characters.")]
        public string LastName { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        [StringLength(254, ErrorMessage = "Email cannot be longer than 254 characters.")]
        public string Email { get; set; } = string.Empty;
        [Required]
        [MaxLength(4, ErrorMessage = "CountryCode cannot be longer than 4 characters.")]
        public string CountryCode { get; set; } = string.Empty;
        [Required]
        [Phone]
        [StringLength(20, ErrorMessage = "Phone cannot be longer than 20 characters.")]
        public string Phone { get; set; } = string.Empty;
        [Required]
        [StringLength(100, ErrorMessage = "Area cannot be longer than 100 characters.")]
        public string Area { get; set; } = string.Empty;
        [Required]
        [StringLength(64, ErrorMessage = "ReferralID cannot be longer than 64 characters.")]
        public string ReferralID { get; set; } = string.Empty;
        [Required]
        [StringLength(50, ErrorMessage = "Experience cannot be longer than 50 characters.")]
        public string Experience { get; set; } = string.Empty;
        [Required]
        [StringLength(50, ErrorMessage = "EnglishLevel cannot be longer than 50 characters.")]
        public string EnglishLevel { get; set; } = string.Empty;
        [Required]
        [StringLength(100, ErrorMessage = "Country cannot be longer than 100 characters.")]
        public string Country { get; set; } = string.Empty;
        [StringLength(100, ErrorMessage = "City cannot be longer than 100 characters.")]
        public string? City { get; set; } = string.Empty;
        [Required]
        [StringLength(250, ErrorMessage = "Account cannot be longer than 250 characters.")]
        public string Account { get; set; } = string.Empty;
        [Required]
        [StringLength(200, ErrorMessage = "HowHear cannot be longer than 200 characters.")]
        public string HowHear { get; set; } = string.Empty;
        [Required]
        [StringLength(1500, ErrorMessage = "Comments cannot be longer than 1500 characters.")]
        public string Comments { get; set; } = string.Empty;
        [StringLength(80, ErrorMessage = "VacancyId cannot be longer than 80 characters.")]
        public string? VacancyId { get; set; } = string.Empty;
        [StringLength(80, ErrorMessage = "ExternalVacancyId cannot be longer than 80 characters.")]
        public string? ExternalVacancyId { get; set; } = string.Empty;
        [StringLength(250, ErrorMessage = "Position cannot be longer than 250 characters.")]
        public string? Position { get; set; } = string.Empty;
        [StringLength(100, ErrorMessage = "VacancyCountry cannot be longer than 100 characters.")]
        public string? VacancyCountry { get; set; } = string.Empty;

        public CreateReferralDto Sanitize()
        {
            return new CreateReferralDto
            {
                FirstName = InputSanitizer.SanitizePlainText(FirstName),
                LastName = InputSanitizer.SanitizePlainText(LastName),
                Email = InputSanitizer.SanitizePlainText(Email).Trim(),
                CountryCode = InputSanitizer.SanitizePlainText(CountryCode),
                Phone = InputSanitizer.SanitizePlainText(Phone),
                Area = InputSanitizer.SanitizePlainText(Area),
                ReferralID = InputSanitizer.SanitizePlainText(ReferralID),
                Experience = InputSanitizer.SanitizePlainText(Experience),
                EnglishLevel = InputSanitizer.SanitizePlainText(EnglishLevel),
                Country = InputSanitizer.SanitizePlainText(Country),
                City = InputSanitizer.SanitizePlainText(City),
                Account = InputSanitizer.SanitizePlainText(Account),
                HowHear = InputSanitizer.SanitizePlainText(HowHear),
                Comments = InputSanitizer.SanitizePlainText(Comments, preserveNewLines: true),
                VacancyId = InputSanitizer.SanitizePlainText(VacancyId),
                ExternalVacancyId = InputSanitizer.SanitizePlainText(ExternalVacancyId),
                Position = InputSanitizer.SanitizePlainText(Position),
                VacancyCountry = InputSanitizer.SanitizePlainText(VacancyCountry)
            };
        }
    }
}
