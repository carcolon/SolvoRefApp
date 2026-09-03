namespace Core.Feature.Referrals.UpdateReferralStatus
{
    public class UpdateReferralStatusDto
    {
        public string Source { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ApplicantStatus { get; set; } = string.Empty;
        public string Ownership { get; set; } = string.Empty;
        public string StatusLead { get; set; } = string.Empty;
        public string ResumeAvailable { get; set; } = string.Empty;
        public string HuntyEnglishScore { get; set; } = string.Empty;
    }
}
