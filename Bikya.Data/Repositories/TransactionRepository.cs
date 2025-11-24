
using Bikya.Data;
using Bikya.Data.Enums;
using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using Bikya.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Bikya.Data.Repositories
{
    public class TransactionRepository : GenericRepository<Transaction>, ITransactionRepository
    {
        private readonly new BikyaContext _context;

        public TransactionRepository(BikyaContext context, ILogger<GenericRepository<Transaction>> logger) : base(context, logger)
        {
            _context = context;
        }

        public async Task<Transaction?> GetTransactionByIdAsync(int transactionId, CancellationToken cancellationToken = default)
        {
            return await _context.Transactions
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);
        }

       

       

        public async Task<List<Transaction>> GetTransactionsByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _context.Transactions
     .AsNoTracking()
     .OrderByDescending(t => t.CreatedAt)
     .ToListAsync(cancellationToken);
        }

        public async Task<List<Transaction>> GetTransactionsByTypeAsync(TransactionType type, CancellationToken cancellationToken = default)
        {
            return await _context.Transactions
                .AsNoTracking()
                .Where(t => t.Type == type)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Transaction>> GetTransactionsByStatusAsync(TransactionStatus status, CancellationToken cancellationToken = default)
        {
            return await _context.Transactions
                .AsNoTracking()
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Transaction>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            return await _context.Transactions
                .AsNoTracking()
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> UpdateTransactionStatusAsync(int transactionId, TransactionStatus status, CancellationToken cancellationToken = default)
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);

            if (transaction == null)
                return false;

            transaction.Status = status;
            _context.Transactions.Update(transaction);
            return true;
        }

        public async Task<decimal> GetTotalAmountByTypeAsync(TransactionType type, CancellationToken cancellationToken = default)
        {
            return await _context.Transactions
                .AsNoTracking()
                .Where(t => t.Type == type && t.Status == TransactionStatus.Completed)
                .SumAsync(t => t.Amount, cancellationToken);
        }

      

        public async Task<int> GetTransactionsCountByTypeAsync(TransactionType type, CancellationToken cancellationToken = default)
        {
            return await _context.Transactions
                .AsNoTracking()
                .CountAsync(t => t.Type == type, cancellationToken);
        }

        public async Task<bool> HasCompletedPaymentTransactionAsync(int transactionId, CancellationToken cancellationToken = default)
        {
            return await _context.Transactions
                .AsNoTracking()
                .AnyAsync(t => t.Id == transactionId &&
                              t.Type == TransactionType.Payment &&
                              t.Status == TransactionStatus.Completed,
                         cancellationToken);
        }

        public override async Task AddAsync(Transaction entity, CancellationToken cancellationToken = default)
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.Status = TransactionStatus.Pending;
            await _context.Transactions.AddAsync(entity, cancellationToken);
        }

        public override void Update(Transaction entity)
        {
            _context.Transactions.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;

            // Preserve CreatedAt field during updates
            _context.Entry(entity).Property(e => e.CreatedAt).IsModified = false;
        }
    }
}