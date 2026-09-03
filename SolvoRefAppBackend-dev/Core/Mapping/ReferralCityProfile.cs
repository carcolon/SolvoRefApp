using AutoMapper;
using Core.Models.Global;
using Core.Models.Referrals;

namespace Core.Mapping
{
    public class ReferralCityProfile : Profile
    {
        public ReferralCityProfile()
        {
            CreateMap<ReferralCity, SelectStructure>()
            .ForMember(des => des.Value, opt => opt.MapFrom(src => src.Description))
            .ForMember(des => des.Text, opt => opt.MapFrom(src => src.Description));
        }
    }
}