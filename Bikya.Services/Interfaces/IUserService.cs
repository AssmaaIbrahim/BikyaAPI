using Bikya.Data.Response;
using Bikya.DTOs.AuthDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bikya.DTOs.UserDTOs;
using Microsoft.AspNetCore.Http;
namespace Bikya.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApiResponse<UserProfileDto>> GetByIdAsync(int id);

        Task<ApiResponse<bool>> UpdateProfileAsync(int userId, UpdateProfileDto dto);

        Task<ApiResponse<bool>> ChangePasswordAsync(int userId, ChangePasswordDto dto);
        Task<ApiResponse<bool>> DeactivateAccountAsync(int userId);
        //Task<ApiResponse<bool>> UpdateProfileImageAsync(int userId, string imageUrl);

        Task<ApiResponse<bool>> ReactivateAccountAsync(string email);

        Task<ApiResponse<UserStatsDTO>> GetUserStatsAsync(int userId);
        Task<ApiResponse<bool>> IsVipSellerAsync(int sellerId);

        Task<ApiResponse<PublicUserProfileDto>> GetPublicUserProfileAsync(int userId,int? currentUserId);
        Task<ApiResponse<string>> UploadProfileImageAsync(int userId, IFormFile imageFile);
        Task<ApiResponse<UserAddressInfoDto>> GetUserAddressInfo(int id);

    }
}
