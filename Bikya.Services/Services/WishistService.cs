using Bikya.Data.Models;
using Bikya.Data.Repositories;
using Bikya.Data.Repositories.Interfaces;
using Bikya.DTOs.ProductDTO;
using Bikya.Services.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bikya.Services.Services
{
    public class WishistService : BaseService
    {
        private readonly IWishlistRepository _wishListRepository;
        private readonly IProductRepository _productRepository;

        public WishistService(
            IWishlistRepository wishListRepository,
            IProductRepository productRepository,
            ILogger<IWishlistRepository> logger,
            UserManager<ApplicationUser> userManager)
            : base(logger, userManager)
        {
            _wishListRepository = wishListRepository ?? throw new ArgumentNullException(nameof(wishListRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        }

        public async Task AddWishListAsync(int productId, int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidatePositiveId(productId, "Product ID");
                await ValidateUserExistsAsync(userId, cancellationToken);

                var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
                ValidateEntityNotNull(product, "Product", productId);

                var existing = await _wishListRepository.GetByUserAndProductAsync(userId, productId, cancellationToken);
                if (existing != null)
                    throw new BusinessException("This product is already in your wishlist.");

                var wish = new WishList
                {
                    ProductId = productId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                };

                await _wishListRepository.CreateAsync(wish, cancellationToken);
                LogInformation("Added product {ProductId} to user {UserId} wishlist", productId, userId);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error adding wishlist for user {UserId} and product {ProductId}", userId, productId);
                throw new BusinessException("An error occurred while adding the product to the wishlist.", ex);
            }
        }

        public async Task RemoveWishListAsync(int productId, int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidatePositiveId(productId, "Product ID");
                await ValidateUserExistsAsync(userId, cancellationToken);

                var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
                ValidateEntityNotNull(product, "Product", productId);

                var existing = await _wishListRepository.GetByUserAndProductAsync(userId, productId, cancellationToken);
                if (existing == null)
                    throw new BusinessException("This product is not in your wishlist.");

                await _wishListRepository.DeleteAsync(existing, cancellationToken);
                LogInformation("Removed product {ProductId} from user {UserId} wishlist", productId, userId);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error removing wishlist for user {UserId} and product {ProductId}", userId, productId);
                throw new BusinessException("An error occurred while removing the product from the wishlist.", ex);
            }
        }

        public async Task<IEnumerable<GetProductDTO>> GetUserWishListAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                await ValidateUserExistsAsync(userId, cancellationToken);
                var products = await _wishListRepository.GetUserWishlistProductsAsync(userId, cancellationToken);
                var wishlist = await _wishListRepository.GetProductIdsByUserIdAsync(userId, cancellationToken);

                //Get just avaliable products but keep it in db
                return products.Where(p=>p.IsApproved&&p.Status==Data.Enums.ProductStatus.Available) .Select(p => p.ToGetProductDTO(wishlist));
              
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error retrieving wishlist for user {UserId}", userId);
                throw new BusinessException("An error occurred while retrieving your wishlist.", ex);
            }
        }

        public async Task<int> GetUserWishListCountAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                await ValidateUserExistsAsync(userId, cancellationToken);
                return await _wishListRepository.CountUserWishlistAsync(userId, cancellationToken);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error getting wishlist count for user {UserId}", userId);
                throw new BusinessException("An error occurred while getting the wishlist count.", ex);
            }
        }
    }
}
