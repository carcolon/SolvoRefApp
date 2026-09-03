using AutoMapper;
using Core.Models.Global;
using Core.Models.Referrals;

namespace Core.Mapping
{
    public class ReferralAccountProfile : Profile
    {
        public ReferralAccountProfile()
        {
            CreateMap<ReferralAccount, SelectStructure>()
            .ForMember(des => des.Value, opt => opt.MapFrom(src => src.Description))
            .ForMember(des => des.Text, opt => opt.MapFrom(src => src.Description));
        }
    }
}