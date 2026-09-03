using System.ComponentModel.DataAnnotations;
using Core.Models.Identity;

namespace Core.Models.Referrals
{
    public class ReferralLink
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(128)]
        public string Token { get; set; } = string.Empty;

        [Required]
        public string ReferrerId { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser Referrer { get; set; } = default!;
    }
}
