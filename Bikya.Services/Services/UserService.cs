using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using Bikya.Data.Response;
using Bikya.DTOs.AuthDTOs;
using Bikya.DTOs.ProductDTO;
using Bikya.DTOs.ReviewDTOs;
using Bikya.DTOs.UserDTOs;
using Bikya.Services.Interfaces;
using Bikya.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Bikya.Services.Services
{
    public class UserService : IUserService
    {

        private readonly IWebHostEnvironment _env;
        // تعريف ثوابت لرسائل الأخطاء
        private const string UserNotFoundMessage = "User not found.";
        private const string PasswordChangeFailedMessage = "Incorrect password or invalid new password.";
        private const string ProfileUpdateFailedMessage = "Failed to update profile.";

        private readonly IUserRepository _userRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IProductService _productService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public UserService(
        IUserRepository userRepository,
        IReviewRepository reviewRepository,
        IProductService productService,
        IWebHostEnvironment env,
        IHttpContextAccessor httpContextAccessor)
        {
            _userRepository = userRepository;
            _reviewRepository = reviewRepository;
            _productService = productService;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Retrieves a user by ID and throws a standardized error response if not found.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>ApplicationUser instance or null</returns>
        private async Task<ApplicationUser?> GetUserOrErrorAsync(int userId)
        {
            return await _userRepository.FindByIdAsync(userId);
        }

        /// <summary>
        /// Changes the password for a user.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="dto">ChangePasswordDto containing current and new password</param>
        /// <returns>ApiResponse indicating success or failure</returns>
        public async Task<ApiResponse<bool>> ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            var user = await GetUserOrErrorAsync(userId);
            if (user == null)
                return ApiResponse<bool>.ErrorResponse(UserNotFoundMessage, 404);

            var result = await _userRepository.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse<bool>.ErrorResponse(PasswordChangeFailedMessage, 400, errors);
            }

            return ApiResponse<bool>.SuccessResponse(true, "Password changed successfully.");
        }

        /// <summary>
        /// Deactivates a user account (soft delete).
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>ApiResponse indicating success or failure</returns>
        public async Task<ApiResponse<bool>> DeactivateAccountAsync(int userId)
        {
            var user = await GetUserOrErrorAsync(userId);
            if (user == null)
                return ApiResponse<bool>.ErrorResponse(UserNotFoundMessage, 404);

            await _userRepository.SoftDeleteUserAsync(userId);
            return ApiResponse<bool>.SuccessResponse(true, "Account deactivated successfully.");
        }

        public async Task<ApiResponse<bool>> ReactivateAccountAsync(string email)
        {
            var user = await _userRepository.FindByEmailAsync(email);
            if (user == null)
                return ApiResponse<bool>.ErrorResponse("User not found", 404);

            if (!user.IsDeleted)
                return ApiResponse<bool>.ErrorResponse("Account is already active", 400);

            user.IsDeleted = false;
            await _userRepository.UpdateUserAsync(user);
            return ApiResponse<bool>.SuccessResponse(true, "Account reactivated successfully.");
        }



        /// <summary>
        /// Gets a user profile by user ID.
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>ApiResponse containing UserProfileDto or error</returns>
        public async Task<ApiResponse<UserProfileDto>> GetByIdAsync(int id)
        {
            var user = await GetUserOrErrorAsync(id);
            if (user == null)
                return ApiResponse<UserProfileDto>.ErrorResponse(UserNotFoundMessage, 404);

            var roles = await _userRepository.GetUserRolesAsync(user);

            return ApiResponse<UserProfileDto>.SuccessResponse(new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                UserName = user.UserName,

                Address = user.Address,
                PostalCode = user.PostalCode,
                City = user.City,
                PhoneNumber = user.PhoneNumber,

                SellerRating = user.SellerRating,
                ProfileImageUrl = user.ProfileImageUrl,
                IsVerified = user.IsVerified,
                Roles = roles.ToList()
            });
        }

        /// <summary>
        /// Updates a user profile.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="dto">UpdateProfileDto containing new profile data</param>
        /// <returns>ApiResponse indicating success or failure</returns>
        public async Task<ApiResponse<bool>> UpdateProfileAsync(int userId, UpdateProfileDto dto)
        {
            var user = await GetUserOrErrorAsync(userId);
            if (user == null)
                return ApiResponse<bool>.ErrorResponse(UserNotFoundMessage, 404);

            user.FullName = dto.FullName ?? user.FullName;
            user.ProfileImageUrl = dto.ProfileImageUrl ?? user.ProfileImageUrl;
            user.Address = dto.Address ?? user.Address;
            user.PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber;
            user.City = dto.City ?? user.City;
            user.PostalCode = dto.PostalCode ?? user.PostalCode;


            var result = await _userRepository.UpdateUserAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse<bool>.ErrorResponse(ProfileUpdateFailedMessage, 400, errors);
            }

            return ApiResponse<bool>.SuccessResponse(true, "Profile updated successfully.");
        }

        public async Task<ApiResponse<UserStatsDTO>> GetUserStatsAsync(int userId)
        {
            var productCount = await _userRepository.CountUserProductsAsync(userId);
            var orderCount = await _userRepository.CountUserOrdersAsync(userId);
            var averageRating = await _userRepository.GetAverageRatingForSellerAsync(userId);
            var totalSales = await _userRepository.CountUserSalesAsync(userId);

            if (productCount == 0 && orderCount == 0 && totalSales == 0 && averageRating == 0)
                return ApiResponse<UserStatsDTO>.ErrorResponse("No stats available for this user.", 404);

            var dto = new UserStatsDTO
            {
                TotalProducts = productCount,
                TotalOrders = orderCount,
                TotalSales = totalSales,
                AvrageReating = averageRating
            };

            return ApiResponse<UserStatsDTO>.SuccessResponse(dto, "User stats retrieved successfully.");
        }

        public async Task<ApiResponse<bool>> IsVipSellerAsync(int sellerId)
        {
            var avgRating = await _userRepository.GetAverageRatingForSellerAsync(sellerId);

            if (avgRating == 5.0)
                return ApiResponse<bool>.SuccessResponse(true, "Seller is VIP.");

            return ApiResponse<bool>.SuccessResponse(false, "Seller is not VIP.");
        }

        public async Task<ApiResponse<PublicUserProfileDto>> GetPublicUserProfileAsync(int userId,int?currentUserId)
        {
            // 1. Get user
            var user = await _userRepository.GetUserWithDetailsAsync(userId);
            if (user == null)
                return ApiResponse<PublicUserProfileDto>.ErrorResponse("User not found", 404);

            // 2. Get stats
            var productCount = await _userRepository.CountUserProductsAsync(userId);
            var orderCount = await _userRepository.CountUserOrdersAsync(userId);
            var averageRating = await _userRepository.GetAverageRatingForSellerAsync(userId);

            // 3. Get reviews
            var reviews = await _reviewRepository.GetReviewsBySellerIdAsync(userId);

            // 4. Get products
            var products = await _productService.GetApprovedProductsByUserAsync(userId,currentUserId);

            // 5. Map to DTO
            var profileDto = new PublicUserProfileDto
            {
                FullName = user.FullName,
                ProfileImageUrl = user.ProfileImageUrl,
                ProductCount = productCount,
                SalesCount = orderCount,
                AverageRating = averageRating,

                Reviews = reviews.Select(r => new ReviewDTO
                {
                    BuyerName = r.Reviewer?.FullName ?? "Unknown",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                }).ToList(),

                ProductsForSale = products.ToList()


            };

            return ApiResponse<PublicUserProfileDto>.SuccessResponse(profileDto);
        }

        public async Task<ApiResponse<string>> UploadProfileImageAsync(int userId, IFormFile imageFile)
        {
            var response = new ApiResponse<string>();

            if (imageFile == null || imageFile.Length == 0)
            {
                response.Success = false;
                response.StatusCode = 400;
                response.Message = "Invalid file.";
                return response;
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "User not found.";
                return response;
            }

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(imageFile.FileName)}";
            var folderPath = Path.Combine(_env.WebRootPath, "Images", "Users");

            // Create folder if it doesn't exist
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var savePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            // 🔥 الحصول على رابط كامل
            var request = _httpContextAccessor.HttpContext.Request;
            var imageUrl = $"{request.Scheme}://{request.Host}/Images/Users/{fileName}";

            // حفظ الرابط الكامل في قاعدة البيانات
            user.ProfileImageUrl = imageUrl;
            await _userRepository.UpdateUserAsync(user);

            response.Success = true;
            response.StatusCode = 200;
            response.Data = imageUrl;
            response.Message = "Profile image uploaded successfully.";
            return response;
        }


        public async Task<ApiResponse<UserAddressInfoDto>> GetUserAddressInfo(int id)
        {
            var user = await GetUserOrErrorAsync(id);
            if (user == null)
                return ApiResponse<UserAddressInfoDto>.ErrorResponse(UserNotFoundMessage, 404);

            return ApiResponse<UserAddressInfoDto>.SuccessResponse(new UserAddressInfoDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Address= user.Address,
                PostalCode = user.PostalCode,
                City = user.City,
                PhoneNumber = user.PhoneNumber,

             
            });
        }
    }
}