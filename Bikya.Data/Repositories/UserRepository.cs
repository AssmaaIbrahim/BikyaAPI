
using Bikya.Data;
using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using Bikya.Data.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Bikya.Data.Repositories
{
    public class UserRepository : GenericRepository<ApplicationUser>, IUserRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly new BikyaContext _context;

        public UserRepository(BikyaContext context, UserManager<ApplicationUser> userManager, ILogger<GenericRepository<ApplicationUser>> logger)
            : base(context, logger)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IQueryable<ApplicationUser>> GetUsersQueryableAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(_userManager.Users.AsQueryable());
        }

        public async Task<List<ApplicationUser>> GetFilteredUsersAsync(string? search, string? status, CancellationToken cancellationToken = default)
        {
            var query = _userManager.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));

            if (status == "active")
                query = query.Where(u => !u.LockoutEnabled && !u.IsDeleted);
            else if (status == "inactive")
                query = query.Where(u => u.LockoutEnabled || u.IsDeleted);

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<List<ApplicationUser>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
        {
            return await GetFilteredUsersAsync(null, "active", cancellationToken);
        }

        public async Task<List<ApplicationUser>> GetInactiveUsersAsync(CancellationToken cancellationToken = default)
        {
            return await GetFilteredUsersAsync(null, "inactive", cancellationToken);
        }

        public async Task<int> GetUsersCountAsync(CancellationToken cancellationToken = default)
        {
            return await _userManager.Users.CountAsync(cancellationToken);
        }

        public async Task<ApplicationUser?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _userManager.FindByIdAsync(id.ToString());
        }

        public async Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<ApplicationUser?> FindByIdStringAsync(string id, CancellationToken cancellationToken = default)
        {
            return await _userManager.FindByIdAsync(id);
        }

        public async Task<IdentityResult> UpdateUserAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            return await _userManager.UpdateAsync(user);
        }

        public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<IdentityResult> RemoveFromRolesAsync(ApplicationUser user, IEnumerable<string> roles, CancellationToken cancellationToken = default)
        {
            return await _userManager.RemoveFromRolesAsync(user, roles);
        }

        public async Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string role, CancellationToken cancellationToken = default)
        {
            return await _userManager.AddToRoleAsync(user, role);
        }

        public async Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            return await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        }

        public async Task SoftDeleteUserAsync(int id, CancellationToken cancellationToken = default)
        {
            var user = await FindByIdAsync(id, cancellationToken);
            if (user != null)
            {
                user.IsDeleted = true;
                await UpdateUserAsync(user, cancellationToken);
            }
        }

        public async Task ReactivateUserAsync(string email, CancellationToken cancellationToken = default)
        {
            var user = await FindByEmailAsync(email, cancellationToken);
            if (user != null)
            {
                user.IsDeleted = false;
                user.LockoutEnd = null;
                await UpdateUserAsync(user, cancellationToken);
            }
        }

        public async Task LockUserAsync(int id, CancellationToken cancellationToken = default)
        {
            var user = await FindByIdAsync(id, cancellationToken);
            if (user != null)
            {
                user.LockoutEnd = DateTimeOffset.MaxValue;
                await UpdateUserAsync(user, cancellationToken);
            }
        }

        public async Task UnlockUserAsync(int id, CancellationToken cancellationToken = default)
        {
            var user = await FindByIdAsync(id, cancellationToken);
            if (user != null)
            {
                user.LockoutEnd = null;
                await UpdateUserAsync(user, cancellationToken);
            }
        }

        public async Task<bool> IsUserLockedAsync(int id, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

            return user?.LockoutEnd > DateTimeOffset.UtcNow;
        }

        public async Task<bool> IsUserDeletedAsync(int id, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

            return user?.IsDeleted ?? false;
        }

        public async Task<IEnumerable<ApplicationUser>> GetUsersByRoleAsync(string roleName, CancellationToken cancellationToken = default)
        {
            return await _userManager.GetUsersInRoleAsync(roleName);
        }

        /// <summary>
        /// Permanently deletes a user by ID (hard delete).
        /// </summary>
        public async Task DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }

        // Override base methods to use UserManager instead of DbContext
        public override async Task<ApplicationUser?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            return await _userManager.FindByIdAsync(id.ToString());
        }

        public override async Task<IEnumerable<ApplicationUser>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _userManager.Users.AsNoTracking().ToListAsync(cancellationToken);
        }

        public override async Task<IEnumerable<ApplicationUser>> FindAsync(Expression<Func<ApplicationUser, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _userManager.Users.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);
        }

        public override async Task<ApplicationUser?> FirstOrDefaultAsync(Expression<Func<ApplicationUser, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _userManager.Users.FirstOrDefaultAsync(predicate, cancellationToken);
        }

        public override async Task AddAsync(ApplicationUser entity, CancellationToken cancellationToken = default)
        {
            await _userManager.CreateAsync(entity);
        }

        public override async Task AddRangeAsync(IEnumerable<ApplicationUser> entities, CancellationToken cancellationToken = default)
        {
            foreach (var entity in entities)
            {
                await _userManager.CreateAsync(entity);
            }
        }

        public override void Update(ApplicationUser entity)
        {
            // UserManager handles tracking automatically
            _ = _userManager.UpdateAsync(entity);
        }

        public override void Remove(ApplicationUser entity)
        {
            _ = _userManager.DeleteAsync(entity);
        }

        public override void RemoveRange(IEnumerable<ApplicationUser> entities)
        {
            foreach (var entity in entities)
            {
                _ = _userManager.DeleteAsync(entity);
            }
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // UserManager automatically saves changes
            return await Task.FromResult(1);
        }

        public override async Task<int> CountAsync(Expression<Func<ApplicationUser, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            if (predicate == null)
                return await _userManager.Users.CountAsync(cancellationToken);

            return await _userManager.Users.CountAsync(predicate, cancellationToken);
        }


        public async Task<int> CountUserProductsAsync(int userId)
        {
            return await _context.Products.CountAsync(p => p.UserId == userId);
        }

        public async Task<int> CountUserOrdersAsync(int userId)
        {
            return await _context.Orders.CountAsync(o => o.SellerId == userId); // أو حسب ما بتسميها
        }

        public async Task<decimal> CountUserSalesAsync(int userId)
        {
            return await _context.Orders
                .Where(o => o.SellerId == userId && o.Status == Enums.OrderStatus.Completed)
                .SumAsync(o => o.TotalAmount); // أو o.TotalPrice حسب اسم العمود
        }

        public async Task<double> GetAverageRatingForSellerAsync(int sellerId)
        {
            return await _context.Reviews
                .Where(r => r.SellerId == sellerId)
                .AverageAsync(r => (double?)r.Rating) ?? 0;
        }
        public async Task UpdateAsync(ApplicationUser user)
        {
            await _userManager.UpdateAsync(user);
        }
        public async Task<ApplicationUser?> GetUserWithDetailsAsync(int userId)
        {
            return await _context.Users
                //.Include(u => u.Products)
                .Include(u => u.ReviewsReceived)
                .Include(u => u.OrdersSold)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        }

    }
}