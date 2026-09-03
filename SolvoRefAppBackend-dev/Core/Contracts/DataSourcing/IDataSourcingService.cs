using Core.Models.DataSourcing;
using Core.Models.Global;

namespace Core.Contracts.DataSourcing
{
    public interface IDataSourcingService
    {
        Task<Response<bool>> Create(DataSourcingTable data);
        Task<Response<object>> GetLeadDiagnostics(string email, string? source);
    }
}
