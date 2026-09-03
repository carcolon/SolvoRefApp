using Core.Feature.Referrals.UpdateReferralStatus;
using Core.Models.Fabric;
using Core.Models.Global;

namespace Core.Contracts.Fabric
{
    public interface IFabricService
    {
        Task<Response<bool>> ReferredValidation(string phone, string email);
        Task<Response<List<UpdateReferralStatusDto>>> GetReferralStatuses(List<string> sources, List<string> emails);
        Task<Response<object>> GetApplicantStatusDiagnostics(string source, string email, bool includeSourceSearch = true);
        Task<Response<object>> GetActiveEmployeeDiagnostics(string personalId);
        Task<Response<object>> GetPeopleHrDiagnostics(string personalId);
        Task<Response<object>> GetPeopleHrTableDiagnostics(string schema, string table, string? personalId);
        Task<Response<List<string>>> GetHuntyEmails(List<string> emails);
        Task<Response<List<UpdateReferralPlacementDto>>> GetReferralPlacements(List<string> emails);
        Task<Response<List<ExtraUser>>> GetActiveEmployeesByPersonalId(List<string> personalIds);
        Task<Response<List<ExtraUser>>> GetEmployeesByPersonalId(List<string> personalIds);
        Task<Response<ExtraUser>> GetExtraUserInformation(string email);
        Task<Response<ExtraUser>> GetExtraUserInformationBySolvoId(string solvoId);
        Task<Response<List<ExtraUser>>> GetExtraUserInformation(List<string> data);
        Task<Response<List<ExtraUser>>> GetExtraUserInformationByPersonalId(List<string> personalIds);
        Task<Response<List<PaymentSchedule>>> GetAllPaymentSchedule();
        Task<Response<List<FabricJobPosting>>> GetActiveJobPostings();
        Task<Response<string>> ExportActiveJobPostingSchemaProfileCsv();
        Task<Response<string>> ExportActiveJobPostingRawCsv();
        Task<Response<FabricConnectionDiagnostics>> GetActiveJobPostingDiagnostics();
    }
}
