using Core.Contracts.Referrals;
using Core.DBContext;
using Core.Models.Global;
using Microsoft.EntityFrameworkCore;

namespace Core.Repositories.Referral
{
    public class PaymentScheduleRepository : IPaymentScheduleRepository
    {
        private readonly SolvoRefAppContext _context;

        public PaymentScheduleRepository(SolvoRefAppContext context)
        {
            _context = context;
        }

        public Task<List<PaymentSchedule>> GetAll()
        {
            return _context.PaymentSchedules.AsNoTracking().ToListAsync();
        }
    }
}
