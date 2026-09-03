using AutoMapper;
using Core.Feature.Referrals.Common;
using Core.Feature.Referrals.CreateReferral;
using Core.Feature.Referrals.GetAllReferrerByUser;
using Core.Models.Referrals;

namespace Core.Mapping
{
    public class ReferralProfile : Profile
    {
        public ReferralProfile()
        {
            CreateMap<CreateReferralDto, Referral>();
            CreateMap<Referral, GetAllReferralByUserDto>()
            .ForMember(des => des.City, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.City) ? "-" : src.City))
            .ForMember(des => des.CreationDate, opt => opt.MapFrom(src => $"{src.CreationDate.ToString("MM-dd-yyyy")}"))
            .ForMember(des => des.Company, opt => opt.MapFrom(src => ReferralCompanyResolver.ResolveDataSourcingCompany(src.Account, src.Area, src.Country, src.City)))
            .ForMember(des => des.IsTransparent, opt => opt.MapFrom(src => ReferralCompanyResolver.IsTransparentReferral(src.Account, src.Area, src.Country, src.City)))
            .ForMember(des => des.Name, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
            CreateMap<QueryReferralUser, QueryReferralBase>();
        }

    }
}
