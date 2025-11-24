using Bikya.Data.Models;
using Bikya.Data.Response;
using Bikya.DTOs.ProductDTO;
using Bikya.Services.Exceptions;
using Bikya.Services.Interfaces;
using Bikya.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bikya.API.Areas.Products.Controller
{
    /// <summary>
    /// Controller for managing product operations.
    /// </summary>
    [Area("Products")]
    [Route("api/[area]/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ProductImageService _productImageService;
        private readonly IWebHostEnvironment _env;

        public ProductController(
            IWebHostEnvironment env,
            IProductService productService,
            ProductImageService productImageService)
        {
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _productImageService = productImageService ?? throw new ArgumentNullException(nameof(productImageService));
        }

        #region Helper Methods

        private bool TryGetUserId(out int userId)
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        }

        //protected async Task<IActionResult> HandleRequest(Func<Task> action, string successMessage)
        //{
        //    try
        //    {
        //        await action();
        //        return Ok(ApiResponse<bool>.SuccessResponse(true, successMessage));
        //    }
        //    catch (ValidationException ex)
        //    {
        //        return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message, 400));
        //    }
        //    catch (ArgumentException ex)
        //    {
        //        return NotFound(ApiResponse<string>.ErrorResponse(ex.Message, 404));
        //    }
        //    catch (Exception)
        //    {
        //        return StatusCode(500, ApiResponse<string>.ErrorResponse("Server error", 500));
        //    }
        //}

        #endregion

        #region Admin Operations

        /// <summary>
        /// Gets all products with images (Admin only).
        /// </summary>
        /// <returns>List of all products</returns>
        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllProductsWithImages()
        {
            try
            {
                var userId = TryGetUserId(out var uid) ? uid : (int?)null;

                var products = await _productService.GetAllProductsWithImagesAsync(userId);
                return Ok(ApiResponse<IEnumerable<GetProductDTO>>.SuccessResponse(products));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<string>.ErrorResponse(ex.Message, 403));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ApiResponse<string>.ErrorResponse(ex.Message, 404));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message, 400));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message, 400));
            }
            catch (BusinessException ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message, 400));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.ErrorResponse("Internal server error", 500));
            }
        }

        /// <summary>
        /// Gets all not approved products with images (Admin only).
        /// </summary>
        /// <returns>List of not approved products</returns>
        //[Authorize(Roles = "Admin")]
        //[HttpGet("not-approved")]
        //public async Task<IActionResult> GetNotApprovedProductsWithImages()
        //{
        //    var products = await _productService.GetNotApprovedProductsWithImagesAsync();
        //    return Ok(ApiResponse<IEnumerable<GetProductDTO>>.SuccessResponse(products));
        //}

        /// <summary>
        /// Approves a product (Admin only).
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Approval result</returns>
        [Authorize(Roles = "Admin")]
        [HttpPost("approve/{id}")]
        public async Task<IActionResult> ApproveProduct(int id)
        {
            try
            {
                await _productService.ApproveProductAsync(id);
                return Ok(ApiResponse<bool>.SuccessResponse(true, "Product approved successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<string>.ErrorResponse(ex.Message, 403));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ApiResponse<string>.ErrorResponse(ex.Message, 404));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message, 400));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message, 400));
            }
            catch (BusinessException ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message, 400));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.ErrorResponse("Internal server error", 500));
            }
        }

        /// <summary>
        /// Rejects a product (Admin only).
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Rejection result</returns>
        [Authorize(Roles = "Admin")]
        [HttpPost("reject/{id}")]
        public async Task<IActionResult> RejectProduct(int id)
        {
            try
            {
                await _productService.RejectProductAsync(id);
                return Ok(ApiResponse<bool>.SuccessResponse(true, "Product rejected successfully"));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message, 400));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ApiResponse<string>.ErrorResponse(ex.Message, 404));
            }
            catch (Exception)
            {
                return StatusCode(500, ApiResponse<string>.ErrorResponse("Server error while rejecting product", 500));
            }
        }

        #endregion

        #region Public Operations

        /// <summary>
        /// Gets all approved products with images.
        /// </summary>
        /// <returns>List of approved products</returns>
        [HttpGet("approved")]
        public async Task<IActionResult> GetApprovedProductsWithImages()
        {
            try
            {
                var userId = TryGetUserId(out var uid) ? uid : (int?)null;
                var products = await _productService.GetApprovedProductsWithImagesAsync(userId);
                return Ok(ApiResponse<IEnumerable<GetProductDTO>>.SuccessResponse(products));
            }
            catch (Exception ex)
            {
                // You can log here again if you want — or rely on service-level logging
                return StatusCode(500, ApiResponse<string>.ErrorResponse("Failed to retrieve approved products", 500));
            }
        }

        /// <summary>
        /// Gets a product by ID with images.
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Product details</returns>
        [HttpGet("Product/{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            try
            {
                var userId = TryGetUserId(out var uid) ? uid : (int?)null;
                var product = await _productService.GetProductWithImagesByIdAsync(id,userId);
                return Ok(ApiResponse<GetProductDTO>.SuccessResponse(product));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message, 400));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ApiResponse<string>.ErrorResponse(ex.Message, 404));
            }
            catch (Exception)
            {
                return StatusCode(500, ApiResponse<string>.ErrorResponse("Failed to retrieve product", 500));
            }
        }

        /// <summary>
        /// Gets products by user ID.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of user's products</returns>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetProductsByUser(int userId)
        {

            try
            {
                var products = await _productService.GetProductsByUserAsync(userId);
                return Ok(ApiResponse<IEnumerable<GetProductDTO>>.SuccessResponse(products));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ApiResponse<string>.ErrorResponse(ex.Message, 404));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message, 400));
            }
            catch (Exception)
            {
                return StatusCode(500, ApiResponse<string>.ErrorResponse("Failed to retrieve products", 500));
            }
        }

        /// <summary>
        /// Gets  approved products by user ID.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of user's not approved products</returns>
        /// 

        [HttpGet("approved/user/{userId}")]
        public async Task<IActionResult> GetApprovedProductsByUser(int userId)
        {
            try
            {
                var products = await _productService.GetApprovedProductsByUserAsync(userId);
                return Ok(ApiResponse<IEnumerable<GetProductDTO>>.SuccessResponse(products));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ApiResponse<string>.ErrorResponse(ex.Message, 404));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message, 400));
            }
            catch (Exception)
            {
                return StatusCode(500, ApiResponse<string>.ErrorResponse("Failed to retrieve approved products for user", 500));
            }
        }

        /// <summary>
        /// Gets products by category ID.
        /// </summary>
        /// <param name="id">Category ID</param>
        /// <returns>List of products in category</returns>
        [HttpGet("category/{id}")]
        public async Task<IActionResult> GetProductsByCategory(int id)
        {
            try
            {
                var userId = TryGetUserId(out var uid) ? uid : (int?)null;
                var products = await _productService.GetProductsByCategoryAsync(id,userId);
                return Ok(ApiResponse<IEnumerable<GetProductDTO>>.SuccessResponse(products));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message, 400));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ApiResponse<string>.ErrorResponse(ex.Message, 404));
            }
            catch (Exception)
            {
                return StatusCode(500, ApiResponse<string>.ErrorResponse("Failed to retrieve products by category", 500));
            }
        }

        #endregion

        #region CRUD Operations

        /// <summary>
        /// Creates a new product.
        /// </summary>
        /// <param name="product">Product data</param>
        /// <returns>Creation result</returns>
        /// 

        //[Authorize]
        //[HttpPost]
        //public async Task<IActionResult> CreateProduct([FromBody] ProductDTO product)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    if (!TryGetUserId(out int userId))
        //        return Unauthorized(ApiResponse<string>.ErrorResponse("Unauthorized", 401));

        //    await _productService.CreateProductAsync(product, userId);
        //    return Ok(ApiResponse<bool>.SuccessResponse(true));
        //}

        /// <summary>
        /// Creates a new product with images.
        /// </summary>
        /// <param name="productDTO">Product data with images</param>
        /// <returns>Creation result</returns>
        [Authorize]
        [Consumes("multipart/form-data")]
        [HttpPost("add")]
        public async Task<IActionResult> CreateProductWithImages([FromForm] CreateProductWithimagesDTO productDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetUserId(out int userId))
                return Unauthorized(ApiResponse<string>.ErrorResponse("Unauthorized", 401));

            var rootPath = _env.WebRootPath;

            try
            {
                var product = new ProductDTO
                {
                    Title = productDTO.Title,
                    Description = productDTO.Description,
                    Price = productDTO.Price,
                    IsForExchange = productDTO.IsForExchange,
                    Condition = productDTO.Condition,
                    CategoryId = productDTO.CategoryId
                };

                var createdProduct = await _productService.CreateProductAsync(product, userId);

                // Upload main image
                if (productDTO.MainImage != null)
                {
                    await _productImageService.AddProductImageAsync(new ProductImageDTO
                    {
                        ProductId = createdProduct.Id,
                        Image = productDTO.MainImage,
                        IsMain = true
                    }, userId, rootPath);
                }

                // Upload additional images
                if (productDTO.AdditionalImages?.Any() == true)
                {
                    foreach (var image in productDTO.AdditionalImages)
                    {
                        await _productImageService.AddProductImageAsync(new ProductImageDTO
                        {
                            ProductId = createdProduct.Id,
                            Image = image,
                            IsMain = false
                        }, userId, rootPath);
                    }
                }

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Product created successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<string>.ErrorResponse(ex.Message, 403));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message, 400));
            }
            catch (IOException ex)
            {
                return StatusCode(500, ApiResponse<string>.ErrorResponse($"Image upload failed: {ex.Message}", 500));
            }
            catch (ConflictException ex)
            {
                return Conflict(ApiResponse<string>.ErrorResponse(ex.Message, 409));
            }
            catch (Exception)
            {
                return StatusCode(500, ApiResponse<string>.ErrorResponse("Internal server error", 500));
            }
        }


        /// <summary>
        /// Updates a product.
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <param name="product">Updated product data</param>
        /// <returns>Update result</returns>
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] ProductDTO product)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetUserId(out int userId))
                return Unauthorized(ApiResponse<string>.ErrorResponse("Unauthorized", 401));

            try
            {
                await _productService.UpdateProductAsync(id, product, userId);
                return Ok(ApiResponse<bool>.SuccessResponse(true, "Product updated successfully"));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message, 400));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<string>.ErrorResponse(ex.Message, 403));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message, 400));
            }
            catch (ConflictException ex)
            {
                return Conflict(ApiResponse<string>.ErrorResponse(ex.Message, 409));
            }
            catch (Exception)
            {
                return StatusCode(500, ApiResponse<string>.ErrorResponse("Internal server error", 500));
            }
        }
        /// <summary>
        /// Deletes a product.
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Deletion result</returns>
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized(ApiResponse<string>.ErrorResponse("Unauthorized", 401));

            var rootPath = _env.WebRootPath;

            try
            {
                await _productService.DeleteProductAsync(id, userId, rootPath);
                return Ok(ApiResponse<bool>.SuccessResponse(true, "Product deleted successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<string>.ErrorResponse(ex.Message, 403));
            }
            catch (ValidationException ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message, 400));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ApiResponse<string>.ErrorResponse(ex.Message, 404));
            }
            catch (Exception)
            {
                return StatusCode(500, ApiResponse<string>.ErrorResponse("Internal server error", 500));
            }
        }

        #endregion

        #region Image Operations

        /// <summary>
        /// Adds an image to a product.
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="dto">Image data</param>
        /// <returns>Image addition result</returns>
        [Authorize]
        [HttpPost("{productId}/images")]
        public async Task<IActionResult> AddImage(int productId, [FromForm] CreateImageDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetUserId(out int userId))
                return Unauthorized(ApiResponse<string>.ErrorResponse("Unauthorized", 401));

            var rootPath = _env.WebRootPath;

            try
            {
                if (dto.Image != null)
                {
                    await _productImageService.AddProductImageAsync(new ProductImageDTO
                    {
                        ProductId = productId,
                        Image = dto.Image,
                        IsMain = dto.IsMain
                    }, userId, rootPath);
                }

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Image uploaded successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<string>.ErrorResponse(ex.Message, 403));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ApiResponse<string>.ErrorResponse(ex.Message, 404));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message, 400));
            }
            catch (IOException ex)
            {
                return StatusCode(500, ApiResponse<string>.ErrorResponse($"Image upload failed: {ex.Message}", 500));
            }
            catch (Exception)
            {
                return StatusCode(500, ApiResponse<string>.ErrorResponse("Internal server error", 500));
            }
        }

        /// <summary>
        /// Deletes a product image.
        /// </summary>
        /// <param name="id">Image ID</param>
        /// <returns>Image deletion result</returns>
        //[Authorize]
        //[HttpPut("image/{id}")]
        //public async Task<IActionResult> updatetImage(int id,[FromForm] UpdatProductImage dto) 
        //    {
        //        if (!TryGetUserId(out int userId))
        //            return Unauthorized(ApiResponse<string>.ErrorResponse("Unauthorized", 401));

        //        var rootPath = _env.WebRootPath;
        //        if (dto.Image != null)
        //        {
        //            await _productImageService.UpdateProductImageAsync(id, userId, rootPath, dto.Image);
        //        }
        //        return Ok(ApiResponse<bool>.SuccessResponse(true));
        //    }


        [Authorize]
        [HttpPut("image/{id}/set-main")]
        public async Task<IActionResult> SetMainImage(int id)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized(ApiResponse<string>.ErrorResponse("Unauthorized", 401));

            try
            {
                await _productImageService.SetMainImageAsync(id, userId);
                return Ok(ApiResponse<bool>.SuccessResponse(true, "Main image updated successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<string>.ErrorResponse(ex.Message, 403));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ApiResponse<string>.ErrorResponse(ex.Message, 404));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message, 400));
            }
            catch (Exception)
            {
                return StatusCode(500, ApiResponse<string>.ErrorResponse("Internal server error", 500));
            }
        }

        /// <summary>
        /// Deletes a product image.
        /// </summary>
        /// <param name="id">Image ID</param>
        /// <returns>Image deletion result</returns>
        [Authorize]
            [HttpDelete("image/{id}")]
            public async Task<IActionResult> DeleteProductImage(int id)
            {
                if (!TryGetUserId(out int userId))
                    return Unauthorized(ApiResponse<string>.ErrorResponse("Unauthorized", 401));

                var rootPath = _env.WebRootPath;
             
            try
            {
                await _productImageService.DeleteProductImageAsync(id, userId, rootPath);
                return Ok(ApiResponse<bool>.SuccessResponse(true));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403,ApiResponse<string>.ErrorResponse(ex.Message, 403));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ApiResponse<string>.ErrorResponse(ex.Message, 404));
            }
            catch (InvalidOperationException ex)
            {
                // Expected business logic error like main image delete
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message, 400));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.ErrorResponse("Internal server error", 500));
            }
        }

            #endregion
        }
    } 

