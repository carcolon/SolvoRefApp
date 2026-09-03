namespace Core.Models.Global
{
    public class PaymentSchedule
    {
        public int Id { get; set; }
        public string Employer { get; set; } = string.Empty;
        public string PaymentFrequency { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public DateTime? DeadLine1 { get; set; }
        public DateTime? PaymentDate1 { get; set; }
        public DateTime? DeadLine2 { get; set; }
        public DateTime? PaymentDate2 { get; set; }
    }
}
