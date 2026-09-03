
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Core.Models.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public string PayrollCompany { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string SolId { get; set; } = string.Empty;
        [MaxLength(64)]
        public string? EmployeeId { get; set; }
        public string PersonalId { get; set; } = string.Empty;
        public string PaymentFrequency { get; set; } = "ME";
        [MaxLength(32)]
        public string SolvoPartnerStatus { get; set; } = string.Empty;
    }
}
