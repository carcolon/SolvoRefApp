using System.ComponentModel.DataAnnotations;

namespace Core.Models.Referrals
{
    public class ReferralCountry
    {
        [Key]
        public int Id { get; set; }
        public required string Description { get; set; }
        public required string PhoneCode { get; set; }
        public required bool Active { get; set; } = true;
    }
}