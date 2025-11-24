using System.Collections.Generic;
using System.Threading.Tasks;
using Bikya.Data.Models;

namespace Bikya.Data.Repositories.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Payment> AddAsync(Payment payment);
        Task<Payment?> GetByIdAsync(int id);
        Task<Payment?> GetByStripeSessionIdAsync(string sessionId); // ?
        Task<IEnumerable<Payment>> GetByUserIdAsync(int userId);
        Task<IEnumerable<Payment>> GetPaymentsByOrderIdAsync(int orderId);

        Task SaveChangesAsync();
    }

}