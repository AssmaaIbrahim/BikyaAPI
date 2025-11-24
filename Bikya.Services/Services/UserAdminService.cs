using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using Bikya.Data.Response;
using Bikya.DTOs.AuthDTOs;
using Bikya.DTOs.UserDTOs;
using Bikya.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.Services.Services
{
    public class UserAdminService : IUserAdminService
    {
        private readonly IUserRepository _userRepository;

        public UserAdminService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        /// <summary>
        /// Gets all users with optional filtering by search and status.
        /// </summary>
        public async Task<ApiResponse<List<UserProfileDto>>> GetAllUsersAsync(string? search, string? status)
        {
            //var users = await _userRepository.GetFilteredUsersAsync(search, status);


            //var UserTasks = users.Select(async u => new UserProfileDto
            //{
            //    Id = u.Id,
            //    Email = u.Email,
            //    FullName = u.FullName,
            //    UserName = u.UserName,
            //    IsLocked = u.LockoutEnd != null && u.LockoutEnd > DateTime.UtcNow,
            //    Roles = await _userRepository.GetUserRolesAsync(u),
            //    CreatedAt = u is { } ? (u.GetType().GetProperty("CreatedAt") != null ? (DateTime)u.GetType().GetProperty("CreatedAt").GetValue(u) : DateTime.MinValue) : DateTime.MinValue
            //});
            //var userDtos = await Task.WhenAll(UserTasks);
            //return ApiResponse<List<UserProfileDto>>.SuccessResponse(userDtos.ToList());
            var users = await _userRepository.GetFilteredUsersAsync(search, status);
            var userDtos = new List<UserProfileDto>();

            foreach (var u in users)
            {
                var roles = await _userRepository.GetUserRolesAsync(u);

                var dto = new UserProfileDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    UserName = u.UserName,
                    IsLocked = u.LockoutEnd != null && u.LockoutEnd > DateTime.UtcNow,
                    Roles = roles,
                    CreatedAt = u is { }
                        ? (u.GetType().GetProperty("CreatedAt") != null
                            ? (DateTime)(u.GetType().GetProperty("CreatedAt")!.GetValue(u) ?? DateTime.MinValue)
                            : DateTime.MinValue)
                        : DateTime.MinValue
                };

                userDtos.Add(dto);
            }

            return ApiResponse<List<UserProfileDto>>.SuccessResponse(userDtos);
        

        }

        /// <summary>
        /// Gets all active users.
        /// </summary>
        public async Task<ApiResponse<List<UserProfileDto>>> GetActiveUsersAsync()
        {
            var users = await _userRepository.GetActiveUsersAsync();

            var userDtos = users.Select(u => new UserProfileDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                UserName = u.UserName
            }).ToList();

            return ApiResponse<List<UserProfileDto>>.SuccessResponse(userDtos);
        }

        /// <summary>
        /// Gets all inactive users.
        /// </summary>
        public async Task<ApiResponse<List<UserProfileDto>>> GetInactiveUsersAsync()
        {
            var users = await _userRepository.GetInactiveUsersAsync();

            var userDtos = users.Select(u => new UserProfileDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                UserName = u.UserName
            }).ToList();

            return ApiResponse<List<UserProfileDto>>.SuccessResponse(userDtos);
        }

        /// <summary>
        /// Gets the total count of users.
        /// </summary>
        public async Task<ApiResponse<int>> GetUsersCountAsync()
        {
            int count = await _userRepository.GetUsersCountAsync();
            return ApiResponse<int>.SuccessResponse(count);
        }

        /// <summary>
        /// Updates a user (profile and role).
        /// </summary>
        public async Task<ApiResponse<string>> UpdateUserAsync(int id, UpdateUserDto dto)
        {
            var user = await _userRepository.FindByIdAsync(id);
            if (user == null)
                return ApiResponse<string>.ErrorResponse("User not found.", 404);

            user.FullName = dto.FullName ?? user.FullName;
            user.PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber;

            var updateResult = await _userRepository.UpdateUserAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = updateResult.Errors.Select(e => e.Description).ToList();
                return ApiResponse<string>.ErrorResponse("Failed to update user.", 400, errors);
            }

            // Role update
            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                var currentRoles = await _userRepository.GetUserRolesAsync(user);
                var removeResult = await _userRepository.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    var errors = removeResult.Errors.Select(e => e.Description).ToList();
                    return ApiResponse<string>.ErrorResponse("Failed to remove old roles.", 400, errors);
                }

                var addResult = await _userRepository.AddToRoleAsync(user, dto.Role);
                if (!addResult.Succeeded)
                {
                    var errors = addResult.Errors.Select(e => e.Description).ToList();
                    return ApiResponse<string>.ErrorResponse("Failed to assign new role.", 400, errors);
                }
            }

            return ApiResponse<string>.SuccessResponse("User updated successfully.");
        }

        /// <summary>
        /// Permanently deletes a user by ID (hard delete).
        /// </summary>
        public async Task<ApiResponse<string>> HardDeleteUserAsync(int id)
        {
            var user = await _userRepository.FindByIdAsync(id);
            if (user == null)
                return ApiResponse<string>.ErrorResponse("User not found.", 404);
            await _userRepository.DeleteUserAsync(id);
            return ApiResponse<string>.SuccessResponse("User permanently deleted.");
        }

        /// <summary>
        /// Deletes (hard delete) a user by ID.
        /// </summary>
        public async Task<ApiResponse<string>> DeleteUserAsync(int id)
        {
            var user = await _userRepository.FindByIdAsync(id);
            if (user == null)
                return ApiResponse<string>.ErrorResponse("User not found.", 404);
            await _userRepository.DeleteUserAsync(id);
            return ApiResponse<string>.SuccessResponse("User deleted successfully.");
        }

        /// <summary>
        /// Reactivates a user by email.
        /// </summary>
        public async Task<ApiResponse<string>> ReactivateUserAsync(string email)
        {
            var user = await _userRepository.FindByEmailAsync(email);
            if (user == null)
                return ApiResponse<string>.ErrorResponse("User not found.", 404);

            await _userRepository.ReactivateUserAsync(email);
            return ApiResponse<string>.SuccessResponse("User reactivated successfully.");
        }

        /// <summary>
        /// Locks a user account by ID.
        /// </summary>
        public async Task<ApiResponse<string>> LockUserAsync(int id)
        {
            var user = await _userRepository.FindByIdAsync(id);
            if (user == null)
                return ApiResponse<string>.ErrorResponse("User not found.", 404);

            await _userRepository.LockUserAsync(id);
            return ApiResponse<string>.SuccessResponse("User locked successfully.");
        }

        /// <summary>
        /// Unlocks a user account by ID.
        /// </summary>
        public async Task<ApiResponse<string>> UnlockUserAsync(int id)
        {
            var user = await _userRepository.FindByIdAsync(id);
            if (user == null)
                return ApiResponse<string>.ErrorResponse("User not found.", 404);

            await _userRepository.UnlockUserAsync(id);
            return ApiResponse<string>.SuccessResponse("User unlocked successfully.");
        }
        public async Task<ApiResponse<string>> AssignRoleAsync(int  userId, string role)
        {
            var user = await _userRepository.FindByIdAsync(userId);
            if (user == null)
                return ApiResponse<string>.ErrorResponse("User not found");

            var roles = await _userRepository.GetUserRolesAsync(user);
            if (roles.Contains(role, StringComparer.OrdinalIgnoreCase))
                return ApiResponse<string>.ErrorResponse($"User already has role '{role}'");

            var result = await _userRepository.AddToRoleAsync(user, role);
            if (!result.Succeeded)
                return ApiResponse<string>.ErrorResponse(string.Join(", ", result.Errors.Select(e => e.Description)));

            return ApiResponse<string>.SuccessResponse($"Role '{role}' assigned to user {userId}");
        }

        public async Task<ApiResponse<string>> RemoveRoleAsync(int userId, string role)
        {
            var user = await _userRepository.FindByIdAsync(userId);
            if (user == null)
                return ApiResponse<string>.ErrorResponse("User not found");
            var roles = await _userRepository.GetUserRolesAsync(user);
            if (!roles.Contains(role, StringComparer.OrdinalIgnoreCase)) 
                return ApiResponse<string>.ErrorResponse($"User does not have role '{role}'");

            var result = await _userRepository.RemoveFromRolesAsync(user, new[] { role });
            if (!result.Succeeded)
                return ApiResponse<string>.ErrorResponse(string.Join(", ", result.Errors.Select(e => e.Description)));

            return ApiResponse<string>.SuccessResponse($"Role '{role}' removed from user {userId}");
        }
    }
}