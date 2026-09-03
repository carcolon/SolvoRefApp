using AutoMapper;
using Core.Models.Global;
using Core.Models.Referrals;

namespace Core.Mapping
{
    public class ReferralFoundProfile : Profile
    {
        public ReferralFoundProfile()
        {
            CreateMap<ReferralFound, SelectStructure>()
            .ForMember(des => des.Value, opt => opt.MapFrom(src => src.Description))
            .ForMember(des => des.Text, opt => opt.MapFrom(src => src.Description));
        }
    }
}