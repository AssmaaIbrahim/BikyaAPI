using Bikya.Data.Enums;
using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Bikya.Data.Repositories
{
    /// <summary>
    /// Repository for managing Product entities with specific business logic.
    /// </summary>
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        private new readonly BikyaContext _context;

        public ProductRepository(BikyaContext context, ILogger<ProductRepository> logger) 
            : base(context, logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #region Specific Repository Methods

        public async Task<IEnumerable<Product>> GetApprovedProductsWithImagesAsync(CancellationToken cancellationToken = default)
        {
            return await GetProductsWithImagesQuery()
                .Where(p => p.IsApproved).Where(p => p.Status == Enums.ProductStatus.Available)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Product>> GetNotApprovedProductsWithImagesAsync(CancellationToken cancellationToken = default)
        {
            return await GetProductsWithImagesQuery()
                .Where(p => !p.IsApproved)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Product>> GetProductsWithImagesAsync(CancellationToken cancellationToken = default)
        {
            return await GetProductsWithImagesQuery()
                .ToListAsync(cancellationToken);
        }
      
        public async Task<Product?> GetProductWithImagesByIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            return await GetProductsWithImagesQuery()
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        }
        public async Task<Product?> GetProductforDeletingAsync(int productId, CancellationToken cancellationToken = default)
        {
            return await _context.Products.Include(p=>p.Images).FirstOrDefaultAsync(p=>p.Id == productId, cancellationToken);
        }

        public async Task<IEnumerable<Product>> GetProductsByUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await GetProductsWithImagesQuery()
                .Where(p => p.UserId == userId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Product>> GetApprovedProductsByUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await GetProductsWithImagesQuery()
                .Where(p => p.UserId == userId && p.IsApproved && p.Status == Enums.ProductStatus.Available)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
        {
            return await GetProductsWithImagesQuery()
                .Where(p => p.CategoryId == categoryId && p.IsApproved && p.Status == Enums.ProductStatus.Available)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ProductExistsWithTitleForUserAsync(int userId, string title, CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .AsNoTracking()
                .AnyAsync(p => p.UserId == userId && p.Title == title, cancellationToken);
        }

        public async Task<int> CreateAsync(Product product, CancellationToken cancellationToken = default)
        {
            product.CreatedAt = DateTime.UtcNow;
            await AddAsync(product, cancellationToken);
            await SaveChangesAsync(cancellationToken);
            return product.Id;
        }

        public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
        {
            Update(product);
            
            // Preserve CreatedAt field during updates
            _context.Entry(product).Property(e => e.CreatedAt).IsModified = false;
            
            await SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Product product, CancellationToken cancellationToken = default)
        {
         
            Remove(product);

            await SaveChangesAsync(cancellationToken);
        }

        public async Task ApproveProductAsync(int productId, CancellationToken cancellationToken = default)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

            if (product != null)
            {
                product.IsApproved = true;
                await SaveChangesAsync(cancellationToken);
            }
        }

        public async Task RejectProductAsync(int productId, CancellationToken cancellationToken = default)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

            if (product != null)
            {
                product.IsApproved = false;
                await SaveChangesAsync(cancellationToken);
            }
        }
        public async Task updateStatus(int productId, ProductStatus productStatus, CancellationToken cancellationToken = default)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

            if (product != null)
            {
                product.Status = productStatus;
                Update(product);
                await SaveChangesAsync(cancellationToken);
            }
        }
        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Creates a base query for products with images and category included.
        /// </summary>
        /// <returns>IQueryable for products with includes</returns>
        private IQueryable<Product> GetProductsWithImagesQuery()
        {
            return _context.Products
                .AsNoTracking()
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt);
        }

        #endregion

        #region Override Generic Methods

        public override async Task<Product?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == (int)id, cancellationToken);
        }

        public override async Task AddAsync(Product entity, CancellationToken cancellationToken = default)
        {
            entity.CreatedAt = DateTime.UtcNow;
            await base.AddAsync(entity, cancellationToken);
        }

        public override void Update(Product entity)
        {
            base.Update(entity);
            
            // Preserve CreatedAt field during updates
            _context.Entry(entity).Property(e => e.CreatedAt).IsModified = false;
        }

        #endregion
    }
}