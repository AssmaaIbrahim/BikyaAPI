using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using System.Linq.Expressions;

namespace Bikya.Data.Repositories.Interfaces
{
    public interface IReviewRepository : IGenericRepository<Review>
    {
        Task<Review?> GetReviewWithAllRelationsAsync(int id, CancellationToken cancellationToken = default);

        Task<List<Review>> GetAllReviewsWithRelationsAsync(CancellationToken cancellationToken = default);

        Task<List<Review>> GetReviewsBySellerIdAsync(int sellerId, CancellationToken cancellationToken = default);

        Task<List<Review>> GetReviewsByReviewerIdAsync(int reviewerId, CancellationToken cancellationToken = default);

        Task<List<Review>> GetReviewsByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);

        Task<Review?> GetExistingReviewForOrderAsync(int orderId, CancellationToken cancellationToken = default);

        Task<bool> HasReviewForOrderAsync(int orderId, CancellationToken cancellationToken = default);

        Task<bool> CanUserReviewOrderAsync(int orderId, int reviewerId, CancellationToken cancellationToken = default);

        Task<double> GetAverageRatingForSellerAsync(int sellerId, CancellationToken cancellationToken = default);

        Task<int> GetReviewsCountForSellerAsync(int sellerId, CancellationToken cancellationToken = default);

        Task<IEnumerable<Review>> GetReviewsByRatingAsync(int rating, CancellationToken cancellationToken = default);

        Task<IEnumerable<Review>> GetRecentReviewsAsync(int limit, CancellationToken cancellationToken = default);

        Task<IEnumerable<Review>> GetReviewsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        Task<bool> IsReviewOwnerAsync(int reviewId, int reviewerId, CancellationToken cancellationToken = default);
    }
}