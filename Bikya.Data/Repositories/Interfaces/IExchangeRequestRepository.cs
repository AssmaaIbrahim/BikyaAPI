using Bikya.Data.Enums;
using Bikya.Data.Models;

namespace Bikya.Data.Repositories.Interfaces
{
    public interface IExchangeRequestRepository : IGenericRepository<ExchangeRequest>
    {
        Task<ExchangeRequest?> GetByIdWithProductsAsync(int id, CancellationToken cancellationToken = default);

        Task<ExchangeRequest?> GetByIdWithProductsAndUsersAsync(int id, CancellationToken cancellationToken = default);

        Task<List<ExchangeRequest>> GetAllWithProductsAsync(CancellationToken cancellationToken = default);

        Task<List<ExchangeRequest>> GetSentRequestsAsync(int senderUserId, CancellationToken cancellationToken = default);

        Task<List<ExchangeRequest>> GetReceivedRequestsAsync(int receiverUserId, CancellationToken cancellationToken = default);

        Task<ExchangeRequest?> GetRequestForApprovalAsync(int requestId, int currentUserId, CancellationToken cancellationToken = default);

        Task<ExchangeRequest?> GetRequestForDeletionAsync(int requestId, int currentUserId, CancellationToken cancellationToken = default);

        Task<bool> UpdateStatusAsync(int requestId, ExchangeStatus status, CancellationToken cancellationToken = default);

        Task<IEnumerable<ExchangeRequest>> GetRequestsByStatusAsync(ExchangeStatus status, CancellationToken cancellationToken = default);

        Task<IEnumerable<ExchangeRequest>> GetRequestsByProductAsync(int productId, CancellationToken cancellationToken = default);

        Task<bool> HasPendingRequestBetweenProductsAsync(int offeredProductId, int requestedProductId, CancellationToken cancellationToken = default);

        Task<int> GetRequestsCountByUserAsync(int userId, CancellationToken cancellationToken = default);

        Task<IEnumerable<ExchangeRequest>> GetRequestsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        Task<ExchangeRequest?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);
    }
}