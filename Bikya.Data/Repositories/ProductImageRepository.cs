
using Bikya.Data;
using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bikya.Data.Repositories
{
    public class ProductImageRepository : GenericRepository<ProductImage>, IProductImageRepository
    {
        private readonly new BikyaContext _context;

        public ProductImageRepository(BikyaContext context, ILogger<GenericRepository<ProductImage>> logger) : base(context, logger)
        {
            _context = context;
        }

        #region Specific Repository Methods
        public async Task<IEnumerable<ProductImage>> GetImagesByProductIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            return await _context.ProductImages
                .AsNoTracking()
                .Where(pi => pi.ProductId == productId)
                .OrderBy(pi => pi.IsMain ? 0 : 1)
                .ThenBy(pi => pi.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<ProductImage?> GetImageByIdWithProductAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.ProductImages
                .AsNoTracking()
                .Include(pi => pi.Product)
                .FirstOrDefaultAsync(pi => pi.Id == id, cancellationToken);
        }

        public async Task UpdateAsync(ProductImage productImage, CancellationToken cancellationToken = default)
        {
            _context.ProductImages.Attach(productImage);
            _context.Entry(productImage).State = EntityState.Modified;

            // Preserve CreatedAt field during updates
            //_context.Entry(productImage).Property(e => e.CreatedAt).IsModified = true;

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(ProductImage productImage, CancellationToken cancellationToken = default)
        {
            _context.ProductImages.Remove(productImage);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteImagesByProductIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            var images = await _context.ProductImages
                .Where(pi => pi.ProductId == productId)
                .ToListAsync(cancellationToken);

            if (images.Any())
            {
                _context.ProductImages.RemoveRange(images);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<bool> UserOwnsImageAsync(int imageId, int userId, CancellationToken cancellationToken = default)
        {
            return await _context.ProductImages
                .AsNoTracking()
                .Include(pi => pi.Product)
                .Where(pi => pi.Id == imageId)
                .AnyAsync(pi => pi.Product.UserId == userId, cancellationToken);
        }

        #region Override Generic Methods with NoTracking for reads and CreatedAt handling
        public override async Task<ProductImage?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            return await _context.ProductImages
                .AsNoTracking()
                .FirstOrDefaultAsync(pi => pi.Id == (int)id, cancellationToken);
        }

        public override async Task AddAsync(ProductImage entity, CancellationToken cancellationToken = default)
        {
            entity.CreatedAt = DateTime.UtcNow;
            await _context.ProductImages.AddAsync(entity, cancellationToken);
        }

        public override void Update(ProductImage entity)
        {
            _context.ProductImages.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;

            // Preserve CreatedAt field during updates
            _context.Entry(entity).Property(e => e.CreatedAt).IsModified = false;
        }
        #endregion
        #endregion
    }
}