using System.ComponentModel.DataAnnotations;
using Core.Models.Identity;

namespace Core.Models.Referrals
{
    public class Referral
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string FirstName { get; set; } = string.Empty;
        [Required]
        public string LastName { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        [MaxLength(4, ErrorMessage = "CountryCode cannot be longer than 4 characters.")]
        public string CountryCode { get; set; } = string.Empty;
        [Required]
        [Phone]
        public string Phone { get; set; } = string.Empty;
        [Required]
        public string Area { get; set; } = string.Empty;
        [Required]
        public string ReferralID { get; set; } = string.Empty;
        [Required]
        public string Experience { get; set; } = string.Empty;
        [Required]
        public string EnglishLevel { get; set; } = string.Empty;
        [Required]
        public string Country { get; set; } = string.Empty;
        public string? City { get; set; } = string.Empty;
        [Required]
        public string Account { get; set; } = string.Empty;
        public string HowHear { get; set; } = string.Empty;
        [Required]
        public string Comments { get; set; } = string.Empty;
        public string Status { get; set; } = "In Progress";
        public string StatusLead { get; set; } = string.Empty;
        [Required]
        public string ReferrerID { get; set; } = string.Empty;
        [MaxLength(64)]
        public string? ReferrerEmployeeId { get; set; }
        [MaxLength(32)]
        public string ReferrerSolvoPartnerStatus { get; set; } = "Inactive";
        public bool ReferralFromSolvoPartner { get; set; }
        [Required]
        [MaxLength(64)]
        public string ReferralSubmissionKey { get; set; } = string.Empty;
        public required ApplicationUser Referrer { get; set; }
        public DateTime CreationDate { get; set; } = DateTime.Now;
        public DateTime? StartDate { get; set; }
        public DateTime FirstPayment { get; set; } = DateTime.MinValue;
        public DateTime SecondPayment { get; set; } = DateTime.MinValue;
        public string PaymentMessage { get; set; } = string.Empty;
        public bool Updatable { get; set; } = true;
    }
}
