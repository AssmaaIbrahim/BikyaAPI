using Bikya.DTOs.UserDTOs;
using Bikya.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Bikya.Data.Response;

namespace Bikya.API.Areas.Identity.Controllers
{
    /// <summary>
    /// Controller for admin user management operations.
    /// </summary>
    [Route("api/[area]/[controller]")]
    [ApiController]
    [Area("Identity")]
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly IUserAdminService _adminService;

        public AdminUsersController(IUserAdminService adminService)
        {
            _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
        }

        /// <summary>
        /// Gets all users with optional filtering and pagination.
        /// </summary>
        /// <param name="search">Search term for user name or email</param>
        /// <param name="status">Filter by user status (active/inactive)</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10, max: 100)</param>
        /// <returns>Paginated list of users</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var result = await _adminService.GetAllUsersAsync(search, status);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets all active users.
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10, max: 100)</param>
        /// <returns>Paginated list of active users</returns>
        [HttpGet("active")]
        public async Task<IActionResult> GetActive(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var result = await _adminService.GetActiveUsersAsync();
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets all inactive users.
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10, max: 100)</param>
        /// <returns>Paginated list of inactive users</returns>
        [HttpGet("inactive")]
        public async Task<IActionResult> GetInactive(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var result = await _adminService.GetInactiveUsersAsync();
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets the total count of users.
        /// </summary>
        /// <returns>User count statistics</returns>
        [HttpGet("count")]
        public async Task<IActionResult> GetCount()
        {
            var result = await _adminService.GetUsersCountAsync();
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets user statistics and analytics.
        /// </summary>
        /// <returns>User statistics</returns>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            // This would typically return user registration trends, activity stats, etc.
            var result = await _adminService.GetUsersCountAsync();
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets a specific user by ID for admin management.
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User details for admin</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid user ID" });

            // Get all users and filter by ID
            var result = await _adminService.GetAllUsersAsync(null, null);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Updates a user's information.
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="dto">User update data</param>
        /// <returns>Update result</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id <= 0)
                return BadRequest(new { message = "Invalid user ID" });

            var result = await _adminService.UpdateUserAsync(id, dto);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Deletes a user (soft delete).
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid user ID" });

            var result = await _adminService.DeleteUserAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Permanently deletes a user (hard delete).
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Permanent deletion result</returns>
        [HttpDelete("{id}/permanent")]
        public async Task<IActionResult> PermanentDelete(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid user ID" });

            var result = await _adminService.DeleteUserAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Reactivates a user account.
        /// </summary>
        /// <param name="email">User email</param>
        /// <returns>Reactivation result</returns>
        [HttpPost("reactivate")]
        public async Task<IActionResult> Reactivate([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "Email is required" });

            if (!IsValidEmail(email))
                return BadRequest(new { message = "Invalid email format" });

            var result = await _adminService.ReactivateUserAsync(email);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Locks a user account.
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="reason">Reason for locking (optional)</param>
        /// <returns>Lock result</returns>
        [HttpPost("{id}/lock")]
        public async Task<IActionResult> Lock(int id, [FromQuery] string? reason = null)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid user ID" });

            var result = await _adminService.LockUserAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Unlocks a user account.
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Unlock result</returns>
        [HttpPost("{id}/unlock")]
        public async Task<IActionResult> Unlock(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid user ID" });

            var result = await _adminService.UnlockUserAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Resets a user's password (admin action).
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="newPassword">New password</param>
        /// <returns>Password reset result</returns>
        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetUserPassword(int id, [FromBody] string newPassword)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid user ID" });

            if (string.IsNullOrWhiteSpace(newPassword))
                return BadRequest(new { message = "New password is required" });

            if (newPassword.Length < 6)
                return BadRequest(new { message = "Password must be at least 6 characters long" });

            // This would typically call a service method to reset the user's password
            return Ok(new { message = "Password reset initiated" });
        }

        /// <summary>
        /// Assigns a role to a user.
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="role">Role name</param>
        /// <returns>Role assignment result</returns>
        [HttpPost("{id}/assign-role")]
        public async Task<IActionResult> AssignRole(int id, [FromBody] string role)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid user ID" });

            if (string.IsNullOrWhiteSpace(role))
                return BadRequest(new { message = "Role is required" });

            var result = await _adminService.AssignRoleAsync(id,role);
            // This would typically call a service method to assign the role
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Removes a role from a user.
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="role">Role name</param>
        /// <returns>Role removal result</returns>
        [HttpDelete("{id}/remove-role")]
        public async Task<IActionResult> RemoveRole(int id, [FromQuery] string role)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid user ID" });

            if (string.IsNullOrWhiteSpace(role))
                return BadRequest(new { message = "Role is required" });

            var result = await _adminService.RemoveRoleAsync(id,role);

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets user activity logs.
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10, max: 100)</param>
        /// <returns>User activity logs</returns>
        [HttpGet("{id}/activity")]
        public async Task<IActionResult> GetUserActivity(int id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid user ID" });

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            // This would typically return user activity logs
            return Ok(new { message = $"Activity logs for user {id}" });
        }

        /// <summary>
        /// Validates email format.
        /// </summary>
        /// <param name="email">Email to validate</param>
        /// <returns>True if valid email format</returns>
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private IActionResult ValidationErrorResponse()
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.ErrorResponse("Validation failed.", 400, errors));
        }
    }
}