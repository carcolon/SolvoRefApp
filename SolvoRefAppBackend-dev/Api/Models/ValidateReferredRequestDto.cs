using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public sealed class ValidateReferredRequestDto
    {
        [StringLength(20, ErrorMessage = "Phone cannot be longer than 20 characters.")]
        public string Phone { get; set; } = string.Empty;
        [EmailAddress]
        [StringLength(254, ErrorMessage = "Email cannot be longer than 254 characters.")]
        public string Email { get; set; } = string.Empty;
        [StringLength(64, ErrorMessage = "ReferralId cannot be longer than 64 characters.")]
        public string ReferralId { get; set; } = string.Empty;
    }
}
