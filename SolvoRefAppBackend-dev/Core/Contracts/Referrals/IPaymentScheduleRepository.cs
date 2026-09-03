using Core.Models.Global;

namespace Core.Contracts.Referrals
{
    public interface IPaymentScheduleRepository
    {
        Task<List<PaymentSchedule>> GetAll();
    }
}
