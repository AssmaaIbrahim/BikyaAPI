using Bikya.Data.Enums;
using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Bikya.Data.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for managing Product entities with specific business logic.
    /// </summary>
    public interface IProductRepository : IGenericRepository<Product>
    {
        /// <summary>
        /// Gets all approved products with their images and category information.
        /// </summary>
        Task<IEnumerable<Product>> GetApprovedProductsWithImagesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all not approved products with their images and category information.
        /// </summary>
        Task<IEnumerable<Product>> GetNotApprovedProductsWithImagesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all products with their images and category information.
        /// </summary>
        Task<IEnumerable<Product>> GetProductsWithImagesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a product by ID with its images and category information.
        /// </summary>
        Task<Product?> GetProductWithImagesByIdAsync(int productId, CancellationToken cancellationToken = default);
        Task<Product?> GetProductforDeletingAsync(int productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all products for a specific user with their images and category information.
        /// </summary>
        Task<IEnumerable<Product>> GetProductsByUserAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all not approved products for a specific user with their images and category information.
        /// </summary>
        Task<IEnumerable<Product>> GetApprovedProductsByUserAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all approved products for a specific category with their images and category information.
        /// </summary>
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a product with the given title exists for the specified user.
        /// </summary>
        Task<bool> ProductExistsWithTitleForUserAsync(int userId, string title, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new product and returns its ID.
        /// </summary>
        Task<int> CreateAsync(Product product, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing product.
        /// </summary>
        Task UpdateAsync(Product product, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a product.
        /// </summary>
        Task DeleteAsync(Product product, CancellationToken cancellationToken = default);

        /// <summary>
        /// Approves a product by setting IsApproved to true.
        /// </summary>
        Task ApproveProductAsync(int productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rejects a product by setting IsApproved to false.
        /// </summary>
        Task RejectProductAsync(int productId, CancellationToken cancellationToken = default);
        Task updateStatus(int productId, ProductStatus productStatus, CancellationToken cancellationToken = default);
    }
}