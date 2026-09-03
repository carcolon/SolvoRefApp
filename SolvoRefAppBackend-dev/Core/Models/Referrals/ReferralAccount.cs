using System.ComponentModel.DataAnnotations;

namespace Core.Models.Referrals
{
    public class ReferralAccount
    {
        [Key]
        public int Id { get; set; }
        public required string Description { get; set; }
        public required bool Active { get; set; } = true;
    }
}