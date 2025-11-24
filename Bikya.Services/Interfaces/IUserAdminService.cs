using Bikya.Data.Response;
using Bikya.DTOs.AuthDTOs;
using Bikya.DTOs.UserDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.Services.Interfaces
{
    public interface IUserAdminService
    {
        Task<ApiResponse<List<UserProfileDto>>> GetAllUsersAsync(string? search, string? status);
        Task<ApiResponse<List<UserProfileDto>>> GetActiveUsersAsync();
        Task<ApiResponse<List<UserProfileDto>>> GetInactiveUsersAsync();
        Task<ApiResponse<int>> GetUsersCountAsync();

        Task<ApiResponse<string>> UpdateUserAsync(int id, UpdateUserDto dto);
        Task<ApiResponse<string>> DeleteUserAsync(int id);

        Task<ApiResponse<string>> ReactivateUserAsync(string email);
        Task<ApiResponse<string>> LockUserAsync(int id);
        Task<ApiResponse<string>> UnlockUserAsync(int id);
        Task<ApiResponse<string>> HardDeleteUserAsync(int id);
        Task<ApiResponse<string>> RemoveRoleAsync(int userId, string role);
        Task<ApiResponse<string>> AssignRoleAsync(int userId, string role);
    }
}
