
using Bikya.Data;
using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using Bikya.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Bikya.Data.Repositories
{
    public class ReviewRepository : GenericRepository<Review>, IReviewRepository
    {
        private readonly new BikyaContext _context;

        public ReviewRepository(BikyaContext context, ILogger<GenericRepository<Review>> logger) : base(context, logger)
        {
            _context = context;
        }

        public async Task<Review?> GetReviewWithAllRelationsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Reviews
                .AsNoTracking()
                .Include(r => r.Reviewer)
                .Include(r => r.Seller)
                .Include(r => r.Order)
                    .ThenInclude(o => o.Product)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        public async Task<List<Review>> GetAllReviewsWithRelationsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Reviews
                .AsNoTracking()
                .Include(r => r.Reviewer)
                .Include(r => r.Seller)
                .Include(r => r.Order)
                    .ThenInclude(o => o.Product)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Review>> GetReviewsBySellerIdAsync(int sellerId, CancellationToken cancellationToken = default)
        {
            return await _context.Reviews
                .AsNoTracking()
                .Include(r => r.Reviewer)
                .Include(r => r.Seller)
                .Include(r => r.Order)
                    .ThenInclude(o => o.Product)
                .Where(r => r.SellerId == sellerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Review>> GetReviewsByReviewerIdAsync(int reviewerId, CancellationToken cancellationToken = default)
        {
            return await _context.Reviews
                .AsNoTracking()
                .Include(r => r.Reviewer)
                .Include(r => r.Seller)
                .Include(r => r.Order)
                    .ThenInclude(o => o.Product)
                .Where(r => r.ReviewerId == reviewerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Review>> GetReviewsByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
        {
            return await _context.Reviews
                .AsNoTracking()
                .Include(r => r.Reviewer)
                .Include(r => r.Seller)
                .Include(r => r.Order)
                    .ThenInclude(o => o.Product)
                .Where(r => r.OrderId == orderId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<Review?> GetExistingReviewForOrderAsync(int orderId, CancellationToken cancellationToken = default)
        {
            return await _context.Reviews
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.OrderId == orderId, cancellationToken);
        }

        public async Task<bool> HasReviewForOrderAsync(int orderId, CancellationToken cancellationToken = default)
        {
            return await _context.Reviews
                .AsNoTracking()
                .AnyAsync(r => r.OrderId == orderId, cancellationToken);
        }

        public async Task<bool> CanUserReviewOrderAsync(int orderId, int reviewerId, CancellationToken cancellationToken = default)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

            return order?.BuyerId == reviewerId;
        }

        public async Task<double> GetAverageRatingForSellerAsync(int sellerId, CancellationToken cancellationToken = default)
        {
            var reviews = await _context.Reviews
                .AsNoTracking()
                .Where(r => r.SellerId == sellerId)
                .ToListAsync(cancellationToken);

            return reviews.Any() ? reviews.Average(r => r.Rating) : 0.0;
        }

        public async Task<int> GetReviewsCountForSellerAsync(int sellerId, CancellationToken cancellationToken = default)
        {
            return await _context.Reviews
                .AsNoTracking()
                .CountAsync(r => r.SellerId == sellerId, cancellationToken);
        }

        public async Task<IEnumerable<Review>> GetReviewsByRatingAsync(int rating, CancellationToken cancellationToken = default)
        {
            return await _context.Reviews
                .AsNoTracking()
                .Include(r => r.Reviewer)
                .Include(r => r.Seller)
                .Include(r => r.Order)
                    .ThenInclude(o => o.Product)
                .Where(r => r.Rating == rating)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Review>> GetRecentReviewsAsync(int limit, CancellationToken cancellationToken = default)
        {
            return await _context.Reviews
                .AsNoTracking()
                .Include(r => r.Reviewer)
                .Include(r => r.Seller)
                .Include(r => r.Order)
                    .ThenInclude(o => o.Product)
                .OrderByDescending(r => r.CreatedAt)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Review>> GetReviewsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            return await _context.Reviews
                .AsNoTracking()
                .Include(r => r.Reviewer)
                .Include(r => r.Seller)
                .Include(r => r.Order)
                    .ThenInclude(o => o.Product)
                .Where(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> IsReviewOwnerAsync(int reviewId, int reviewerId, CancellationToken cancellationToken = default)
        {
            return await _context.Reviews
                .AsNoTracking()
                .AnyAsync(r => r.Id == reviewId && r.ReviewerId == reviewerId, cancellationToken);
        }

        public override async Task AddAsync(Review entity, CancellationToken cancellationToken = default)
        {
            entity.CreatedAt = DateTime.UtcNow;
            await _context.Reviews.AddAsync(entity, cancellationToken);
        }

        public override void Update(Review entity)
        {
            _context.Reviews.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;

            // Preserve CreatedAt field during updates
            _context.Entry(entity).Property(e => e.CreatedAt).IsModified = false;
        }
    }
}