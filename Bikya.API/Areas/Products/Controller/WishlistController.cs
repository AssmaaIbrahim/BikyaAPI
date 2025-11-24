using Bikya.Data.Response;
using Bikya.DTOs.ProductDTO;
using Bikya.Services.Exceptions;
using Bikya.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bikya.API.Areas.Products.Controller
{
    [Area("Products")]
    [Route("api/[area]/[controller]")]
    [ApiController]
    public class WishlistController : ControllerBase
    {
        private readonly WishistService _wishistService;
        private readonly IWebHostEnvironment _env;

        public WishlistController(
            IWebHostEnvironment env,

            WishistService wishistService,
            ProductImageService productImageService)
        {
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _wishistService = wishistService ?? throw new ArgumentNullException(nameof(wishistService));
        }
        #region Helper Methods

        private bool TryGetUserId(out int userId)
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        }

        #endregion

        [Authorize]
        [HttpPost("add/{productId}")]
        public async Task<IActionResult> AddToWishlist(int productId, CancellationToken cancellationToken)
        {
            try
            {
                if (!TryGetUserId(out int userId))
                    return Unauthorized(ApiResponse<string>.ErrorResponse("Unauthorized", 401));

                await _wishistService.AddWishListAsync(productId, userId, cancellationToken);
                var count = await _wishistService.GetUserWishListCountAsync(userId, cancellationToken);
                return Ok(ApiResponse<int>.SuccessResponse(count));
            }
            catch (BusinessException ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message,400));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.ErrorResponse($"An unexpected error occurred. {ex.Message}", 500));
            }
        }
        [Authorize]
        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> RemoveFromWishlist(int productId, CancellationToken cancellationToken)
        {
            try
            {
                if (!TryGetUserId(out int userId))
                    return Unauthorized(ApiResponse<string>.ErrorResponse("Unauthorized", 401));

                await _wishistService.RemoveWishListAsync(productId, userId, cancellationToken);

                var count = await _wishistService.GetUserWishListCountAsync(userId, cancellationToken);
                return Ok(ApiResponse<int>.SuccessResponse(count));
            }
            catch (BusinessException ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.ErrorResponse($"An unexpected error occurred. {ex.Message}", 500));
            }
        }
        [Authorize]
        [HttpGet("getProduct")]
        public async Task<IActionResult> GetUserWishlist(CancellationToken cancellationToken)
        {
            try
            {
                if (!TryGetUserId(out int userId))
                    return Unauthorized(ApiResponse<string>.ErrorResponse("Unauthorized", 401));

                var wishlist = await _wishistService.GetUserWishListAsync(userId, cancellationToken);
                return Ok(ApiResponse<IEnumerable<GetProductDTO>>.SuccessResponse(wishlist));
            }
            catch (BusinessException ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.ErrorResponse($"Failed to fetch wishlist {ex.Message}", 500));
            }
        }
        [Authorize]
        [HttpGet("count")]
        public async Task<IActionResult> GetUserWishlistCount(CancellationToken cancellationToken)
        {
            try
            {
                if (!TryGetUserId(out int userId))
                    return Unauthorized(ApiResponse<string>.ErrorResponse("Unauthorized", 401));

                int count = await _wishistService.GetUserWishListCountAsync(userId, cancellationToken);
                return Ok(ApiResponse<int>.SuccessResponse(count));
            }
            catch (BusinessException ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.ErrorResponse($"Filed to get wishlist count {ex.Message}", 500));
            }
        }

    }
}
