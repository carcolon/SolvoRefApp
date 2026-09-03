namespace Core.Models.Global
{
    public class ExtraUser
    {
        public string Status { get; set; } = "Active";
        public string PayrollCompany { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string SolId { get; set; } = string.Empty;
        public string PersonalId { get; set; } = string.Empty;
         public string Email { get; set; } = string.Empty;
        public DateTime StartDate { get; set; } = DateTime.MinValue;
        public string PayrollFrequencyClassification { get; set; } = string.Empty;
    }
}