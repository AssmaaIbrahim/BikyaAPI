using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using Bikya.DTOs.ProductDTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bikya.Services.Services
{
    public class ProductImageService
    {
        private readonly IProductImageRepository _productImageRepository;
        private readonly IProductRepository _productRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductImageService(
            IProductImageRepository productImageRepository,
            IProductRepository productRepository,
            UserManager<ApplicationUser> userManager)
        {
            _productImageRepository = productImageRepository ?? throw new ArgumentNullException(nameof(productImageRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        #region Helper Methods
        public async Task<bool> IsAdminAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            var roles = await _userManager.GetRolesAsync(user);
            return roles.Contains("Admin");
        }

        private async Task<bool> UserOwnsProductAsync(int productId, int userId, CancellationToken cancellationToken = default)
        {
            var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
            return product?.UserId == userId;
        }

        private async Task<bool> CanUserModifyProductAsync(int productId, int userId, CancellationToken cancellationToken = default)
        {
            var isAdmin = await IsAdminAsync(userId, cancellationToken);
            var ownsProduct = await UserOwnsProductAsync(productId, userId, cancellationToken);
            return isAdmin || ownsProduct;
        }

        private static void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        private static void DeleteFileIfExists(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        #endregion

        #region CRUD Operations
        public async Task<ProductImage> AddProductImageAsync(ProductImageDTO productImageDTO, int userId, string rootPath, CancellationToken cancellationToken = default)
        {
            if (productImageDTO == null)
                throw new ArgumentNullException(nameof(productImageDTO));

            // Validate product exists
            var product = await _productRepository.GetByIdAsync(productImageDTO.ProductId, cancellationToken);
            if (product == null)
                throw new ArgumentException("Product not found");

            // Check authorization
            if (!await CanUserModifyProductAsync(productImageDTO.ProductId, userId, cancellationToken))
                throw new UnauthorizedAccessException("You do not have permission to add images to this product");

            // Check product status
            if (product.Status != Data.Enums.ProductStatus.Available)
                throw new InvalidOperationException("You cannot modify images for a product that is in Process");

                var safeFileName = productImageDTO.Image.FileName
               .Replace(" ", "-")
               .Replace("(", "")
               .Replace(")", "")
               .Replace("&", "")
               .Replace("#", "")
               .Replace("%", "");

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}_{safeFileName}";
            var folderPath = Path.Combine(rootPath, "Images", "Products");
            var filePath = Path.Combine(folderPath, fileName);

            // Ensure directory exists
            EnsureDirectoryExists(folderPath);

            // Save file to disk with error handling
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await productImageDTO.Image.CopyToAsync(stream, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to save product image to disk: {ex.Message}");
            }

            //make all images not main if this image main
            if (productImageDTO.IsMain)
            {
                var existingImages = await _productImageRepository.GetImagesByProductIdAsync(productImageDTO.ProductId, cancellationToken);

                foreach (var img in existingImages)
                {
                    if (img.IsMain)
                    {
                        img.IsMain = false;
                        await _productImageRepository.UpdateAsync(img, cancellationToken);
                    }
                }
            }

            // Create ProductImage entity (store only the relative path, not the image itself)
            var image = new ProductImage
            {
                ProductId = productImageDTO.ProductId,
                ImageUrl = $"/Images/Products/{fileName}", // فقط المسار النسبي
                IsMain = productImageDTO.IsMain
            };
            if (product.IsApproved)
            {
                product.IsApproved = false;
                await _productRepository.UpdateAsync(product, cancellationToken);
            }

            // Save to database
            await _productImageRepository.AddAsync(image, cancellationToken);
            await _productImageRepository.SaveChangesAsync(cancellationToken);

            return image;
        }

        public async Task<IEnumerable<ProductImage>> GetImagesByProductIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
            if (product == null)
                throw new ArgumentException("Product not found");

            return await _productImageRepository.GetImagesByProductIdAsync(productId, cancellationToken);
        }

        public async Task<ProductImage> GetImageByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var image = await _productImageRepository.GetByIdAsync(id, cancellationToken);
            if (image == null)
                throw new ArgumentException("Product image not found");

            return image;
        }

        public async Task<ProductImage> GetImageByIdWithProductAsync(int id, CancellationToken cancellationToken = default)
        {
            var image = await _productImageRepository.GetImageByIdWithProductAsync(id, cancellationToken);
            if (image == null)
                throw new ArgumentException("Product image not found");

            return image;
        }
       
        public async Task UpdateProductImageAsync(int id, int userId, string rootPath, IFormFile ImageFile, CancellationToken cancellationToken = default)
        {
            if (ImageFile == null) 
                throw new ArgumentNullException(nameof(ImageFile));
            // Validate Image exists
            var productImage = await _productImageRepository.GetByIdAsync(id, cancellationToken);
            if (productImage == null)
                throw new ArgumentException("Product Image not found");


            // Validate product exists
            var product = await _productRepository.GetByIdAsync(productImage.ProductId, cancellationToken);
            if (product == null)
                throw new ArgumentException("Product not found");

            // Check authorization
            if (!await CanUserModifyProductAsync(productImage.ProductId, userId, cancellationToken))
                throw new UnauthorizedAccessException("You do not have permission to update this product image");

            // Check product status
            if (product.Status != Data.Enums.ProductStatus.Available)
                throw new InvalidOperationException("You cannot modify images for a product that is in Process");


            var safeFileName = ImageFile.FileName
              .Replace(" ", "-")
              .Replace("(", "")
              .Replace(")", "")
              .Replace("&", "")
              .Replace("#", "")
              .Replace("%", "");

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}_{safeFileName}";
            var folderPath = Path.Combine(rootPath, "Images", "Products");
            var filePath = Path.Combine(folderPath, fileName);

            // Ensure directory exists
            EnsureDirectoryExists(folderPath);

            // Save file to disk with error handling
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to save product image to disk: {ex.Message}");
            }
           
            // delete old image from disk 
            var oldFileName = productImage?.ImageUrl.TrimStart('/');
            var oldFilePath = Path.Combine(rootPath, oldFileName);
            DeleteFileIfExists(oldFilePath);



            // Create ProductImage entity (store only the relative path, not the image itself)
            var image = new ProductImage
            {
                Id = id,
                ProductId = productImage.ProductId,
                ImageUrl = $"/Images/Products/{fileName}", 
                IsMain = productImage.IsMain,
                CreatedAt = DateTime.Now,

            };

       

            await _productImageRepository.UpdateAsync(image, cancellationToken);
        }

        public async Task DeleteProductImageAsync(int id, int userId, string rootPath, CancellationToken cancellationToken = default)
        {
            var productImage = await _productImageRepository.GetImageByIdWithProductAsync(id, cancellationToken);
            if (productImage == null)
                throw new ArgumentException("Product image not found");

            // Check authorization
            if (!await CanUserModifyProductAsync(productImage.ProductId, userId, cancellationToken))
                throw new UnauthorizedAccessException("You do not have permission to delete this product image");

            // Check product status
            if (productImage.Product.Status != Data.Enums.ProductStatus.Available)
                throw new InvalidOperationException("You cannot modify images for a product that is in Process");

            //checked if it is main
            if (productImage.IsMain ==true )
                throw new InvalidOperationException("You cannot delete main images for a product");

            // Delete file from disk
            var fileName = productImage.ImageUrl.TrimStart('/');
            var filePath = Path.Combine(rootPath, fileName);
            DeleteFileIfExists(filePath);

            // Delete from database
            await _productImageRepository.DeleteAsync(productImage, cancellationToken);
        }

        public async Task DeleteAllProductImagesAsync(int productId, int userId, string rootPath, CancellationToken cancellationToken = default)
        {
            var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
            if (product == null)
                throw new ArgumentException("Product not found");

            // Check authorization
            if (!await CanUserModifyProductAsync(productId, userId, cancellationToken))
                throw new UnauthorizedAccessException("You do not have permission to delete images for this product");

            // Get all images for the product
            var images = await _productImageRepository.GetImagesByProductIdAsync(productId, cancellationToken);

            // Delete files from disk
            foreach (var image in images)
            {
                var fileName = image.ImageUrl.TrimStart('/');
                var filePath = Path.Combine(rootPath, fileName);
                DeleteFileIfExists(filePath);
            }

            // Delete from database
            await _productImageRepository.DeleteImagesByProductIdAsync(productId, cancellationToken);
        }

        public async Task SetMainImageAsync(int imageId, int userId, CancellationToken cancellationToken = default)
        {
            var image = await _productImageRepository.GetImageByIdWithProductAsync(imageId, cancellationToken);
            if (image == null)
                throw new ArgumentException("Product image not found");

            // Check authorization
            if (!await CanUserModifyProductAsync(image.ProductId, userId, cancellationToken))
                throw new UnauthorizedAccessException("You do not have permission to modify this product image");

            // Check product status
            if (image.Product.Status != Data.Enums.ProductStatus.Available)
                throw new InvalidOperationException("You cannot modify images for a product that is in Process");

            // Get all images for the product
            var allImages = await _productImageRepository.GetImagesByProductIdAsync(image.ProductId, cancellationToken);

            // Set all images as not main
            foreach (var img in allImages)
            {
                if (img.IsMain && img.Id != image.Id)
                {
                    img.IsMain = false;
                    await _productImageRepository.UpdateAsync(img, cancellationToken);
                }
            }

            // Set the selected image as main
            image.IsMain = true;
            await _productImageRepository.UpdateAsync(image, cancellationToken);
        }

        public async Task<ProductImage?> GetMainImageByProductIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            var images = await _productImageRepository.GetImagesByProductIdAsync(productId, cancellationToken);
            return images.FirstOrDefault(img => img.IsMain);
        }

        public async Task<int> GetImageCountByProductIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            var images = await _productImageRepository.GetImagesByProductIdAsync(productId, cancellationToken);
            return images.Count();
        }

        public async Task<bool> UserOwnsImageAsync(int imageId, int userId, CancellationToken cancellationToken = default)
        {
            return await _productImageRepository.UserOwnsImageAsync(imageId, userId, cancellationToken);
        }
        #endregion
    }
}