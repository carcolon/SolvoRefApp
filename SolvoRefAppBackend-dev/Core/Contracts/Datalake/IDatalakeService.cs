using Core.Models.Datalake;
using Core.Models.Global;

namespace Core.Contracts.Datalake
{
    public interface IDatalakeService
    {
        Task<Response<EmployeeInfoCheck>> GetEmployeeInfoForCheck(string personalId, string email);
    }
}