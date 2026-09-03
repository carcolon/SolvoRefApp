namespace Core.Feature.Referrals.GetAllReferrerByUser
{
    public class QueryReferralUser
    {
        public string Status { get; set; } = string.Empty;
        [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "PageNumber must be greater than 0.")]
        public int PageNumber { get; set; } = 1;
        [System.ComponentModel.DataAnnotations.Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
        public int PageSize { get; set; } = 20;
    }
}
