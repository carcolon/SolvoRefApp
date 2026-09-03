namespace Core.Feature.Referrals.GetAllReferrerByUser
{
    public class GetAllReferralByUserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string ReferralID { get; set; } = string.Empty;
        public string Experience { get; set; } = string.Empty;
        public string EnglishLevel { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Account { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public bool IsTransparent { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusLead { get; set; } = string.Empty;
        public string CreationDate { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public string PaymentMessage { get; set; } = string.Empty;
    }
}
