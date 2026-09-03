
using AutoMapper;
using Core.Feature.Referrals.Common;
using Core.Models.DataSourcing;
using Core.Models.Referrals;
using System.Globalization;

namespace Core.Mapping
{
    public class DataSourcingProfile : Profile
    {
        public DataSourcingProfile()
        {
            CreateMap<Referral, DataSourcingTable>()
            .ForMember(des => des.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
            .ForMember(des => des.PhoneNumber, opt => opt.MapFrom(src => $"{src.CountryCode}{src.Phone}".Replace(" ", "")))
            .ForMember(des => des.DNI, opt => opt.MapFrom(src => src.ReferralID))
            .ForMember(des => des.ApplyArea, opt => opt.MapFrom(src => src.Area))
            .ForMember(des => des.Company, opt => opt.MapFrom(src => src.Account))
            .ForMember(des => des.Fecha, opt => opt.MapFrom(src => src.CreationDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)))
            .ForMember(des => des.ReferrerEmployeeId, opt => opt.MapFrom(src => src.ReferrerEmployeeId ?? string.Empty))
            .ForMember(des => des.ReferrerSolvoPartnerStatus, opt => opt.MapFrom(src => src.ReferrerSolvoPartnerStatus))
            .ForMember(des => des.ReferralFromSolvoPartner, opt => opt.MapFrom(src => src.ReferralFromSolvoPartner ? "Yes" : "No"))
            .ForMember(des => des.Source, opt => opt.MapFrom(src => ReferralDataSourcingConstants.Source));
        }
    }
}
