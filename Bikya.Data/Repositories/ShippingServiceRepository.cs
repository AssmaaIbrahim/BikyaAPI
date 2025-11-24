
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
    public class ShippingServiceRepository : GenericRepository<ShippingInfo>, IShippingServiceRepository
    {
        private readonly new BikyaContext _context;

        public ShippingServiceRepository(BikyaContext context, ILogger<GenericRepository<ShippingInfo>> logger) : base(context, logger)
        {
            _context = context;
        }

        public async Task<List<ShippingInfo>> GetAllWithOrderByAsync(CancellationToken cancellationToken = default)
        {
            return await _context.ShippingInfos
                .AsNoTracking()
                .OrderByDescending(s => s.CreateAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<ShippingInfo?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default)
        {
            return await _context.ShippingInfos
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ShippingId.ToString() == trackingNumber, cancellationToken);
        }

        public async Task<bool> ValidateOrderOwnershipAsync(int orderId, int userId, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .AsNoTracking()
                .AnyAsync(o => o.Id == orderId && o.BuyerId == userId, cancellationToken);
        }

        public async Task<Order?> GetOrderForShippingAsync(int orderId, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .AsNoTracking()
                .Select(o => new Order
                {
                    Id = o.Id,
                    BuyerId = o.BuyerId,
                    SellerId = o.SellerId,
                    Status = o.Status
                })
                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        }

        public async Task<bool> UpdateShippingStatusAsync(int shippingId, ShippingStatus status, CancellationToken cancellationToken = default)
        {
            var shipping = await _context.ShippingInfos.FindAsync(new object[] { shippingId }, cancellationToken);
            if (shipping == null)
                return false;

            shipping.Status = status;
            _context.ShippingInfos.Update(shipping);
            return true;
        }

        public async Task<IEnumerable<ShippingInfo>> GetShippingsByStatusAsync(ShippingStatus status, CancellationToken cancellationToken = default)
        {
            return await _context.ShippingInfos
                .AsNoTracking()
                .Where(s => s.Status == status)
                .OrderByDescending(s => s.CreateAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ShippingInfo>> GetShippingsByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
        {
            return await _context.ShippingInfos
                .AsNoTracking()
                .Where(s => s.OrderId == orderId)
                .OrderByDescending(s => s.CreateAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ShippingInfo>> GetShippingsByUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _context.ShippingInfos
                .AsNoTracking()
                .Include(s => s.Order)
                .Where(s => s.Order != null && s.Order.BuyerId == userId)
                .OrderByDescending(s => s.CreateAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ShippingInfo>> GetShippingsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            return await _context.ShippingInfos
                .AsNoTracking()
                .Where(s => s.CreateAt >= startDate && s.CreateAt <= endDate)
                .OrderByDescending(s => s.CreateAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> HasExistingShippingAsync(int orderId, CancellationToken cancellationToken = default)
        {
            return await _context.ShippingInfos
                .AsNoTracking()
                .AnyAsync(s => s.OrderId == orderId, cancellationToken);
        }

        public async Task<int> GetShippingsCountByStatusAsync(ShippingStatus status, CancellationToken cancellationToken = default)
        {
            return await _context.ShippingInfos
                .AsNoTracking()
                .CountAsync(s => s.Status == status, cancellationToken);
        }

        public async Task<decimal> GetTotalShippingCostAsync(CancellationToken cancellationToken = default)
        {
            // This would require a shipping cost field in the model
            // For now, returning 0 as placeholder
            return await Task.FromResult(0m);
        }

        public override async Task AddAsync(ShippingInfo entity, CancellationToken cancellationToken = default)
        {
            entity.CreateAt = DateTime.UtcNow;
            entity.Status = ShippingStatus.Pending;
            await _context.ShippingInfos.AddAsync(entity, cancellationToken);
        }

        public override void Update(ShippingInfo entity)
        {
            _context.ShippingInfos.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;

            // Preserve CreateAt field during updates
            _context.Entry(entity).Property(e => e.CreateAt).IsModified = false;
        }
    }
}