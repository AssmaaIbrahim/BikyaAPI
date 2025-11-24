using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Linq.Expressions;

namespace Bikya.Data.Repositories.Interfaces
{
    public interface IUserRepository : IGenericRepository<ApplicationUser>
    {
        Task<IQueryable<ApplicationUser>> GetUsersQueryableAsync(CancellationToken cancellationToken = default);

        Task<List<ApplicationUser>> GetFilteredUsersAsync(string? search, string? status, CancellationToken cancellationToken = default);

        Task<List<ApplicationUser>> GetActiveUsersAsync(CancellationToken cancellationToken = default);

        Task<List<ApplicationUser>> GetInactiveUsersAsync(CancellationToken cancellationToken = default);

        Task<int> GetUsersCountAsync(CancellationToken cancellationToken = default);

        Task<ApplicationUser?> FindByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<ApplicationUser?> FindByIdStringAsync(string id, CancellationToken cancellationToken = default);

        Task<IdentityResult> UpdateUserAsync(ApplicationUser user, CancellationToken cancellationToken = default);

        Task<IList<string>> GetUserRolesAsync(ApplicationUser user, CancellationToken cancellationToken = default);

        Task<IdentityResult> RemoveFromRolesAsync(ApplicationUser user, IEnumerable<string> roles, CancellationToken cancellationToken = default);

        Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string role, CancellationToken cancellationToken = default);

        Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

        Task SoftDeleteUserAsync(int id, CancellationToken cancellationToken = default);

        Task ReactivateUserAsync(string email, CancellationToken cancellationToken = default);

        Task LockUserAsync(int id, CancellationToken cancellationToken = default);

        Task UnlockUserAsync(int id, CancellationToken cancellationToken = default);

        Task<bool> IsUserLockedAsync(int id, CancellationToken cancellationToken = default);

        Task<bool> IsUserDeletedAsync(int id, CancellationToken cancellationToken = default);

        Task<IEnumerable<ApplicationUser>> GetUsersByRoleAsync(string roleName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Permanently deletes a user by ID (hard delete).
        /// </summary>
        Task DeleteUserAsync(int id);
        Task<int> CountUserProductsAsync(int userId);
        Task<int> CountUserOrdersAsync(int userId);
        Task<decimal> CountUserSalesAsync(int userId);
        Task<double> GetAverageRatingForSellerAsync(int sellerId);
        Task UpdateAsync(ApplicationUser user);
        Task<ApplicationUser?> GetUserWithDetailsAsync(int userId);
    }
}