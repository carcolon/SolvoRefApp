using System.ComponentModel.DataAnnotations;

namespace Core.Models.Referrals
{
    public class ReferralCity
    {
        [Key]
        public int Id { get; set; }
        public required string Description { get; set; }
        public required bool Active { get; set; } = true;
        public int CountryId { get; set; }
        public ReferralCountry Country { get; set; }
    }
}