using Bikya.Data.Enums;
using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using System.Linq.Expressions;

namespace Bikya.Data.Repositories.Interfaces
{
    public interface ITransactionRepository : IGenericRepository<Transaction>
    {
        Task<Transaction?> GetTransactionByIdAsync(int transactionId, CancellationToken cancellationToken = default);

        Task<List<Transaction>> GetTransactionsByTypeAsync(TransactionType type, CancellationToken cancellationToken = default);

        Task<List<Transaction>> GetTransactionsByStatusAsync(TransactionStatus status, CancellationToken cancellationToken = default);

        Task<List<Transaction>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        Task<bool> UpdateTransactionStatusAsync(int transactionId, TransactionStatus status, CancellationToken cancellationToken = default);

        Task<decimal> GetTotalAmountByTypeAsync(TransactionType type, CancellationToken cancellationToken = default);


        Task<int> GetTransactionsCountByTypeAsync(TransactionType type, CancellationToken cancellationToken = default);

        Task<bool> HasCompletedPaymentTransactionAsync(int transactionId, CancellationToken cancellationToken = default);
    }
}