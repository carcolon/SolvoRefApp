namespace Core.Feature.Referrals.GetActiveVacancies
{
    public class GetActiveVacanciesDto
    {
        public string PositionName {get; set;} = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string VacancyId {get; set;} = string.Empty;
    }
}
