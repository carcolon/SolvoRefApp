namespace Core.Models.Referrals{
    public class ReferralVacancy
    {
        public int Id {get; set;}
        public string ExternalVacancyId { get; set; } = string.Empty;
        public string PositionName {get; set;} = string.Empty;
        public bool Active {get; set;}
        public string Country {get; set;} = string.Empty;
    }
}
