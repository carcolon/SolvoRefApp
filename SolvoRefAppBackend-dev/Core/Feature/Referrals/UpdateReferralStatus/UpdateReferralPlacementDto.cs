namespace Core.Feature.Referrals.UpdateReferralStatus
{
    public class UpdateReferralPlacementDto
    {
        public string Email { get; set; } = string.Empty;
        public DateTime PlacementCreatedOn { get; set; }
    }
}
