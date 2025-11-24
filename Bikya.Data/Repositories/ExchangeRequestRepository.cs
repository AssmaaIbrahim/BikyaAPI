using Bikya.Data.Enums;
using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bikya.Data.Repositories
{
    public class ExchangeRequestRepository : GenericRepository<ExchangeRequest>, IExchangeRequestRepository
    {
        private new readonly BikyaContext _context;

        public ExchangeRequestRepository(BikyaContext context, ILogger<ExchangeRequestRepository> logger) 
            : base(context, logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<ExchangeRequest?> GetByIdWithProductsAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.ExchangeRequests
                    .AsNoTracking()
                    .Include(e => e.OfferedProduct)                    
                        .ThenInclude(p => p.Images)
                    .Include(e => e.OfferedProduct)
                        .ThenInclude(p => p.Category)
                    .Include(e => e.OfferedProduct)
                        .ThenInclude(p => p.User)
                    .Include(e => e.RequestedProduct)
                        .ThenInclude(p => p.Images)
                    .Include(e => e.RequestedProduct)
                        .ThenInclude(p => p.Category)
                    .Include(e => e.RequestedProduct)
                        .ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving exchange request with ID {RequestId} and products", id);
                throw;
            }
        }

        public async Task<ExchangeRequest?> GetByIdWithProductsAndUsersAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.ExchangeRequests
                    .Include(e => e.OfferedProduct)
                        .ThenInclude(p => p.User)
                    .Include(e => e.OfferedProduct)
                        .ThenInclude(p => p.Images)
                    .Include(e => e.OfferedProduct)
                        .ThenInclude(p => p.Category)
                    .Include(e => e.RequestedProduct)
                        .ThenInclude(p => p.User)
                    .Include(e => e.RequestedProduct)
                        .ThenInclude(p => p.Images)
                    .Include(e => e.RequestedProduct)
                        .ThenInclude(p => p.Category)
                    .Include(e => e.StatusHistory)
                    .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving exchange request with ID {RequestId}, products, and users", id);
                throw;
            }
        }

        public async Task<List<ExchangeRequest>> GetAllWithProductsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.ExchangeRequests
                    .AsNoTracking()
                    .Include(e => e.OfferedProduct)
                        .ThenInclude(p => p.Images)
                    .Include(e => e.RequestedProduct)
                        .ThenInclude(p => p.Images)
                    .OrderByDescending(e => e.RequestedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all exchange requests with products");
                throw;
            }
        }

        public async Task<List<ExchangeRequest>> GetSentRequestsAsync(int senderUserId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.ExchangeRequests
                    .AsNoTracking()
                    .Include(e => e.OfferedProduct)
                        .ThenInclude(p => p.Images)
                    .Include(e => e.RequestedProduct)
                        .ThenInclude(p => p.Images)
                    .Where(e => e.OfferedProduct != null && e.OfferedProduct.UserId.HasValue && e.OfferedProduct.UserId.Value == senderUserId)
                    .OrderByDescending(e => e.RequestedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sent requests for user {UserId}", senderUserId);
                throw;
            }
        }

        public async Task<List<ExchangeRequest>> GetReceivedRequestsAsync(int receiverUserId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.ExchangeRequests
                    .AsNoTracking()
                    .Include(e => e.OfferedProduct)
                        .ThenInclude(p => p.Images)
                    .Include(e => e.RequestedProduct)
                        .ThenInclude(p => p.Images)
                    .Where(e => e.RequestedProduct != null && e.RequestedProduct.UserId.HasValue && e.RequestedProduct.UserId.Value == receiverUserId)
                    .OrderByDescending(e => e.RequestedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving received requests for user {UserId}", receiverUserId);
                throw;
            }
        }

        public async Task<ExchangeRequest?> GetRequestForApprovalAsync(int requestId, int currentUserId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.ExchangeRequests
                    .Include(e => e.RequestedProduct)
                    .FirstOrDefaultAsync(e => e.Id == requestId && e.RequestedProduct.UserId == currentUserId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving request for approval {RequestId} by user {UserId}", requestId, currentUserId);
                throw;
            }
        }

        public async Task<ExchangeRequest?> GetRequestForDeletionAsync(int requestId, int currentUserId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.ExchangeRequests
                    .Include(e => e.OfferedProduct)
                    .Include(e => e.RequestedProduct)
                    .FirstOrDefaultAsync(e => e.Id == requestId &&
                        (e.OfferedProduct.UserId == currentUserId || e.RequestedProduct.UserId == currentUserId),
                        cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving request for deletion {RequestId} by user {UserId}", requestId, currentUserId);
                throw;
            }
        }

        public async Task<bool> UpdateStatusAsync(int requestId, ExchangeStatus status, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = await _context.ExchangeRequests.FindAsync(new object[] { requestId }, cancellationToken);
                if (request == null)
                    return false;

                request.Status = status;
                _context.ExchangeRequests.Update(request);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for request {RequestId} to {Status}", requestId, status);
                throw;
            }
        }

        public async Task<IEnumerable<ExchangeRequest>> GetRequestsByStatusAsync(ExchangeStatus status, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.ExchangeRequests
                    .AsNoTracking()
                    .Include(e => e.OfferedProduct)
                        .ThenInclude(p => p.Images)
                    .Include(e => e.RequestedProduct)
                        .ThenInclude(p => p.Images)
                    .Where(e => e.Status == status)
                    .OrderByDescending(e => e.RequestedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving requests by status {Status}", status);
                throw;
            }
        }

        public async Task<IEnumerable<ExchangeRequest>> GetRequestsByProductAsync(int productId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.ExchangeRequests
                    .AsNoTracking()
                    .Include(e => e.OfferedProduct)
                        .ThenInclude(p => p.Images)
                    .Include(e => e.RequestedProduct)
                        .ThenInclude(p => p.Images)
                    .Where(e => e.OfferedProductId == productId || e.RequestedProductId == productId)
                    .OrderByDescending(e => e.RequestedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving requests by product {ProductId}", productId);
                throw;
            }
        }

        public async Task<bool> HasPendingRequestBetweenProductsAsync(int offeredProductId, int requestedProductId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.ExchangeRequests
                    .AsNoTracking()
                    .AnyAsync(e => e.OfferedProductId == offeredProductId &&
                                  e.RequestedProductId == requestedProductId &&
                                  e.Status == ExchangeStatus.Pending,
                             cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking pending request between products {OfferedProductId} and {RequestedProductId}", offeredProductId, requestedProductId);
                throw;
            }
        }

        public async Task<int> GetRequestsCountByUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.ExchangeRequests
                    .AsNoTracking()
                    .Include(e => e.OfferedProduct)
                    .Include(e => e.RequestedProduct)
                    .CountAsync(e => e.OfferedProduct.UserId == userId || e.RequestedProduct.UserId == userId,
                               cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting requests count for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<ExchangeRequest>> GetRequestsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.ExchangeRequests
                    .AsNoTracking()
                    .Include(e => e.OfferedProduct)
                        .ThenInclude(p => p.Images)
                    .Include(e => e.RequestedProduct)
                        .ThenInclude(p => p.Images)
                    .Where(e => e.RequestedAt >= startDate && e.RequestedAt <= endDate)
                    .OrderByDescending(e => e.RequestedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving requests by date range {StartDate} to {EndDate}", startDate, endDate);
                throw;
            }
        }

        public override async Task AddAsync(ExchangeRequest entity, CancellationToken cancellationToken = default)
        {
            try
            {
                entity.RequestedAt = DateTime.UtcNow;
                entity.Status = ExchangeStatus.Pending;
                await base.AddAsync(entity, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding exchange request");
                throw;
            }
        }

        public override void Update(ExchangeRequest entity)
        {
            try
            {
                base.Update(entity);
                
                // Preserve RequestedAt field during updates
                _context.Entry(entity).Property(e => e.RequestedAt).IsModified = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating exchange request {RequestId}", entity.Id);
                throw;
            }
        }

        public async Task<ExchangeRequest?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.ExchangeRequests
                    .AsNoTracking()
                    .Include(e => e.OfferedProduct)
                    .Include(e => e.RequestedProduct)
                    .FirstOrDefaultAsync(e => e.OrderForOfferedProductId == orderId || e.OrderForRequestedProductId == orderId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving exchange request by order ID {OrderId}", orderId);
                throw;
            }
        }
    }
}