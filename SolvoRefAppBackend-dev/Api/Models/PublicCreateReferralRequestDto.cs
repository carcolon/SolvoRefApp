using System.ComponentModel.DataAnnotations;
using Core.Feature.Referrals.CreateReferral;

namespace Api.Models
{
    public sealed class PublicCreateReferralRequestDto
    {
        [Required]
        public CreateReferralDto Referral { get; set; } = new();

        [Required]
        public string TurnstileToken { get; set; } = string.Empty;
    }
}
