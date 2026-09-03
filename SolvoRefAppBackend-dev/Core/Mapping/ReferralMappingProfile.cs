using AutoMapper;
using Core.Models.Configurations;
using Core.Feature.Referrals.GetActiveVacancies;
using Core.Models.Referrals;

namespace Core.Mapping
{
    public class ReferralMappingProfile : Profile
    {
        public ReferralMappingProfile()
        {
            CreateMap<ReferralVacancy, GetActiveVacanciesDto>()
            .ForMember(dest => dest.PositionName, opt => opt.MapFrom(src => src.PositionName))
            .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Country))
            .ForMember(dest => dest.VacancyId, opt => opt.MapFrom(src => src.ExternalVacancyId));
        }
    }
}
