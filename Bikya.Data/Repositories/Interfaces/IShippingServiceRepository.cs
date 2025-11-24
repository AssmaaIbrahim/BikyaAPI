using Bikya.Data.Enums;
using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using System.Linq.Expressions;

namespace Bikya.Data.Repositories.Interfaces
{
    public interface IShippingServiceRepository : IGenericRepository<ShippingInfo>
    {
        Task<List<ShippingInfo>> GetAllWithOrderByAsync(CancellationToken cancellationToken = default);

        Task<ShippingInfo?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default);

        Task<bool> ValidateOrderOwnershipAsync(int orderId, int userId, CancellationToken cancellationToken = default);

        Task<Order?> GetOrderForShippingAsync(int orderId, CancellationToken cancellationToken = default);

        Task<bool> UpdateShippingStatusAsync(int shippingId, ShippingStatus status, CancellationToken cancellationToken = default);

        Task<IEnumerable<ShippingInfo>> GetShippingsByStatusAsync(ShippingStatus status, CancellationToken cancellationToken = default);

        Task<IEnumerable<ShippingInfo>> GetShippingsByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);

        Task<IEnumerable<ShippingInfo>> GetShippingsByUserAsync(int userId, CancellationToken cancellationToken = default);

        Task<IEnumerable<ShippingInfo>> GetShippingsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        Task<bool> HasExistingShippingAsync(int orderId, CancellationToken cancellationToken = default);

        Task<int> GetShippingsCountByStatusAsync(ShippingStatus status, CancellationToken cancellationToken = default);

        Task<decimal> GetTotalShippingCostAsync(CancellationToken cancellationToken = default);
    }
}