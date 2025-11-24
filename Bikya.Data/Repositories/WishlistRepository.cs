using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bikya.Data.Repositories
{
    public class WishlistRepository : GenericRepository<WishList>, IWishlistRepository
    {

        private readonly BikyaContext _context;

        public WishlistRepository(BikyaContext context, ILogger<WishlistRepository> logger)
            : base(context, logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task CreateAsync(WishList wish, CancellationToken cancellationToken = default)
        {
        

            await AddAsync(wish, cancellationToken);
            await SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(WishList wish, CancellationToken cancellationToken = default)
        {
            
                Remove(wish);
                await SaveChangesAsync(cancellationToken);
            
        }

        public async Task<IEnumerable<Product>> GetUserWishlistProductsAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.User)
                .Where(p => p.Wishlists.Any(w => w.UserId == userId))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountUserWishlistAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _context.WishLists
                .CountAsync(w => w.UserId == userId, cancellationToken);
        }
        public async Task<WishList?> GetByUserAndProductAsync(int userId, int productId, CancellationToken cancellationToken = default)
        {
            return await _context.WishLists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, cancellationToken);
        }
        public async Task<bool> ExistsAsync(int userId, int productId)
        {
            return await _context.WishLists
                .AnyAsync(w => w.UserId == userId && w.ProductId == productId);
        }
        public async Task<HashSet<int>> GetProductIdsByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var ids= await _context.WishLists
                .Where(w => w.UserId == userId)
                .Select(w => w.ProductId).ToListAsync(cancellationToken);
             return new HashSet<int>(ids);
        }
        public async Task RemoveProductFromAllWishlistsAsync(int productId, CancellationToken cancellationToken = default)
        {
            var wishlists = await _context.WishLists
                .Where(w => w.ProductId == productId)
                .ToListAsync();

            if (wishlists.Any())
            {
                _context.WishLists.RemoveRange(wishlists);
                await _context.SaveChangesAsync();
            }
        }
    }
}

