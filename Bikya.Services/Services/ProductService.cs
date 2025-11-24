//using Bikya.Data.Migrations;
using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using Bikya.DTOs.ProductDTO;
using Bikya.Services.Exceptions;
using Bikya.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Bikya.Services.Services
{
    /// <summary>
    /// Service for managing product business logic.
    /// </summary>
    public class ProductService : BaseService, IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IProductImageRepository _productImageRepository;
        private readonly ProductImageService _productImageService;
        private readonly IWishlistRepository _wishlistRepository;

        public ProductService(
            IProductRepository productRepository,
            IProductImageRepository productImageRepository,
            ProductImageService productImageService,
            IWishlistRepository wishlistRepository,
            ILogger<ProductService> logger,
            UserManager<ApplicationUser> userManager) 
            : base(logger, userManager)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _productImageRepository = productImageRepository ?? throw new ArgumentNullException(nameof(productImageRepository));
            _productImageService = productImageService ?? throw new ArgumentNullException(nameof(productImageService));
            _wishlistRepository = wishlistRepository ?? throw new ArgumentNullException(nameof(wishlistRepository));
        }

        #region GET Methods

        public async Task<IEnumerable<GetProductDTO>> GetApprovedProductsWithImagesAsync(int? userId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var products= await _productRepository.GetApprovedProductsWithImagesAsync(cancellationToken);

               

                return await MapProductsWithWishlistAsync(products,userId,cancellationToken);
            }
            catch (Exception ex)
            {
                LogError(ex, "Error retrieving approved products with images");
                throw;
            }
        }

        public async Task<IEnumerable<GetProductDTO>> GetNotApprovedProductsWithImagesAsync(int? userId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var products= await _productRepository.GetNotApprovedProductsWithImagesAsync(cancellationToken);
                return await MapProductsWithWishlistAsync(products, userId, cancellationToken);
            }
            catch (Exception ex)
            {
                LogError(ex, "Error retrieving not approved products with images");
                throw;
            }
        }

        public async Task<IEnumerable<GetProductDTO>> GetAllProductsWithImagesAsync(int? userId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var products= await _productRepository.GetProductsWithImagesAsync(cancellationToken);
                return await MapProductsWithWishlistAsync(products, userId, cancellationToken);
            }
            catch (Exception ex)
            {
                LogError(ex, "Error retrieving all products with images");
                throw;
            }
        }

        public async Task<GetProductDTO> GetProductWithImagesByIdAsync(int productId, int? userId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidatePositiveId(productId, "Product ID");

                var product = await _productRepository.GetProductWithImagesByIdAsync(productId, cancellationToken);
                ValidateEntityNotNull(product, "Product", productId);
                HashSet<int>? wishlistProductIds = null;

                if (userId.HasValue)
                {
                    wishlistProductIds = await _wishlistRepository
                        .GetProductIdsByUserIdAsync(userId.Value, cancellationToken);
                }

                return product.ToGetProductDTO(wishlistProductIds);


               
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error retrieving product with ID {ProductId}", productId);
                throw;
            }
        }

        public async Task<IEnumerable<GetProductDTO>> GetProductsByUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                await ValidateUserExistsAsync(userId, cancellationToken);
                var products= await _productRepository.GetProductsByUserAsync(userId, cancellationToken);
                return await MapProductsWithWishlistAsync(products, userId, cancellationToken);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error retrieving products for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<GetProductDTO>> GetApprovedProductsByUserAsync(int userId,int? currrentUser=null, CancellationToken cancellationToken = default)
        {
            try
            {
                await ValidateUserExistsAsync(userId, cancellationToken);
                var products= await _productRepository.GetApprovedProductsByUserAsync(userId, cancellationToken);

                return await MapProductsWithWishlistAsync(products, currrentUser, cancellationToken);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error retrieving  approved products for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<GetProductDTO>> GetProductsByCategoryAsync(int categoryId, int? userId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidatePositiveId(categoryId, "Category ID");
                var products= await _productRepository.GetProductsByCategoryAsync(categoryId, cancellationToken);
                return await MapProductsWithWishlistAsync(products, userId, cancellationToken);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error retrieving products for category {CategoryId}", categoryId);
                throw;
            }
        }

        #endregion

        #region CRUD Operations

        public async Task<Product> CreateProductAsync(ProductDTO productDTO, int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateProductDTO(productDTO);
                await ValidateUserExistsAsync(userId, cancellationToken);

                var productExists = await _productRepository.ProductExistsWithTitleForUserAsync(userId, productDTO.Title, cancellationToken);
                if (productExists)
                {
                    LogWarning("Product with title '{Title}' already exists for user {UserId}", productDTO.Title, userId);
                    throw new ConflictException($"You already added a product with title '{productDTO.Title}'");
                }

                var product = new Product
                {
                    Title = TrimAndValidateString(productDTO.Title, "Product title"),
                    Description = TrimNullableString(productDTO.Description),
                    Price = productDTO.Price,
                    IsForExchange = productDTO.IsForExchange,
                    Condition = TrimAndValidateString(productDTO.Condition, "Product condition"),
                    CategoryId = productDTO.CategoryId,
                    IsApproved = false,
                    Status = Data.Enums.ProductStatus.Available,
                    UserId = userId
                };

                await _productRepository.CreateAsync(product, cancellationToken);
                
                LogInformation("Product '{Title}' created successfully for user {UserId}", product.Title, userId);
                return product;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error creating product for user {UserId}", userId);
                throw;
            }
        }

        public async Task UpdateProductAsync(int id, ProductDTO productDTO, int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateProductDTO(productDTO);
                ValidatePositiveId(id, "Product ID");
                await ValidateUserExistsAsync(userId, cancellationToken);

                var existing = await _productRepository.GetByIdAsync(id, cancellationToken);
                ValidateEntityNotNull(existing, "Product", id);

                await ValidateUserPermissionAsync(userId, existing.UserId!.Value, cancellationToken);

                if (existing.Status != Data.Enums.ProductStatus.Available)
                {
                    LogWarning("Attempted to update product {ProductId} that is not available", id);
                    throw new ValidationException($"You cannot update a product that is in {existing.Status} status");
                }

                existing.Title = TrimAndValidateString(productDTO.Title, "Product title");
                existing.Description = TrimNullableString(productDTO.Description);
                existing.Price = productDTO.Price;
                existing.IsForExchange = productDTO.IsForExchange;
                existing.Condition = TrimAndValidateString(productDTO.Condition, "Product condition");
                existing.CategoryId = productDTO.CategoryId;
                existing.IsApproved = false; // Reset approval status when product is updated

                await _productRepository.UpdateAsync(existing, cancellationToken);
                
                LogInformation("Product {ProductId} updated successfully by user {UserId}", id, userId);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error updating product {ProductId} by user {UserId}", id, userId);
                throw;
            }
        }

        public async Task DeleteProductAsync(int id, int userId, string rootPath, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidatePositiveId(id, "Product ID");
                await ValidateUserExistsAsync(userId, cancellationToken);

                var existing = await _productRepository.GetProductforDeletingAsync(id, cancellationToken);
                ValidateEntityNotNull(existing, "Product", id);

                await ValidateUserPermissionAsync(userId, existing.UserId!.Value, cancellationToken);

                if (existing.Status != Data.Enums.ProductStatus.Available)
                {
                    LogWarning("Attempted to delete product {ProductId} that is not available", id);
                    throw new ValidationException($"You cannot delete a product that is in {existing.Status} status");
                }


                // Delete associated images first
                await _productImageService.DeleteAllProductImagesAsync(id, userId, rootPath);


                
                await _productRepository.DeleteAsync(existing, cancellationToken);
                
                LogInformation("Product {ProductId} deleted successfully by user {UserId}", id, userId);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error deleting product {ProductId} by user {UserId}", id, userId);
                throw;
            }
        }

        public async Task ApproveProductAsync(int productId, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidatePositiveId(productId, "Product ID");

                var product = await _productRepository.GetProductWithImagesByIdAsync(productId, cancellationToken);
                ValidateEntityNotNull(product, "Product", productId);

                if (product.IsApproved)
                {
                    LogWarning("Product {ProductId} is already approved", productId);
                    throw new ValidationException($"Product {productId} is already approved");
                }

                await _productRepository.ApproveProductAsync(productId, cancellationToken);
                
                LogInformation("Product {ProductId} approved successfully", productId);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error approving product {ProductId}", productId);
                throw;
            }
        }

        public async Task RejectProductAsync(int productId, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidatePositiveId(productId, "Product ID");

                var product = await _productRepository.GetProductWithImagesByIdAsync(productId, cancellationToken);
                ValidateEntityNotNull(product, "Product", productId);

                if (!product.IsApproved)
                {
                    LogWarning("Product {ProductId} is already not approved", productId);
                    throw new ValidationException($"Product {productId} is already not approved");
                }

                await _productRepository.RejectProductAsync(productId, cancellationToken);
                
                LogInformation("Product {ProductId} rejected successfully", productId);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error rejecting product {ProductId}", productId);
                throw;
            }
        }

        #endregion

        #region Private Helper Methods

        private void ValidateProductDTO(ProductDTO productDTO)
        {
            if (productDTO == null)
                throw new ArgumentNullException(nameof(productDTO));

            ValidateRequiredString(productDTO.Title, "Product title");
            ValidateNonNegativePrice(productDTO.Price, "Product price");
            ValidateRequiredString(productDTO.Condition, "Product condition");
            ValidatePositiveId(productDTO.CategoryId, "Category ID");
        }


        private async Task<IEnumerable<GetProductDTO>> MapProductsWithWishlistAsync( IEnumerable<Product> products, int? userId, CancellationToken cancellationToken)
        {
            

            if (userId.HasValue)
            {
                var wishlist = await _wishlistRepository.GetProductIdsByUserIdAsync(userId.Value, cancellationToken);
                return products.Select(p => p.ToGetProductDTO(wishlist));
            }
            return products.Select(p => p.ToGetProductDTO());

        }
        #endregion
    }
}