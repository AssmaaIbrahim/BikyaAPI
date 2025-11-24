using Bikya.DTOs.UserDTOs;
using Bikya.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Bikya.Data.Response;

namespace Bikya.API.Areas.Identity.Controllers
{
    /// <summary>
    /// Controller for managing user operations and profile management.
    /// </summary>
    [Route("api/[area]/[controller]")]
    [ApiController]
    [Area("Identity")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        /// <summary>
        /// Gets the current user's profile information.
        /// </summary>
        /// <returns>Current user's profile data</returns>
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(new { message = "Invalid user token" });

            var result = await _userService.GetByIdAsync(userId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets a user by ID (only accessible by the user themselves or admins).
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User information</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid user ID" });

            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return Unauthorized(new { message = "Invalid user token" });

            // Users can only access their own profile unless they're admin
            if (currentUserId != id && !User.IsInRole("Admin"))
                return Forbid();

            var result = await _userService.GetByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Updates the current user's profile.
        /// </summary>
        /// <param name="dto">Profile update data</param>
        /// <returns>Update result</returns>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse();

            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(new { message = "Invalid user token" });

            var result = await _userService.UpdateProfileAsync(userId, dto);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Changes the current user's password.
        /// </summary>
        /// <param name="dto">Password change data</param>
        /// <returns>Password change result</returns>
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse();

            if (dto.NewPassword != dto.ConfirmNewPassword)
                return BadRequest(new { message = "New password and confirmation password do not match" });

            if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
                return BadRequest(new { message = "Current password is required" });

            if (string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest(new { message = "New password is required" });

            if (dto.NewPassword.Length < 6)
                return BadRequest(new { message = "New password must be at least 6 characters long" });

            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(new { message = "Invalid user token" });

            var result = await _userService.ChangePasswordAsync(userId, dto);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Deactivates the current user's account.
        /// </summary>
        /// <returns>Deactivation result</returns>
        [HttpDelete("deactivate")]
        public async Task<IActionResult> Deactivate()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(new { message = "Invalid user token" });

            var result = await _userService.DeactivateAccountAsync(userId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("reactivate")]
        [AllowAnonymous]
        public async Task<IActionResult> Reactivate([FromBody] ReactivateAccountDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { message = "Email is required" });

            var result = await _userService.ReactivateAccountAsync(dto.Email);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{userId}/stats")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserStats(int userId)
        {
            var result = await _userService.GetUserStatsAsync(userId);
            return StatusCode(result.StatusCode, result);
        }



        /// <summary>
        /// Gets the current user's activity status.
        /// </summary>
        /// <returns>User activity status</returns>
        [HttpGet("status")]
        public async Task<IActionResult> GetUserStatus()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(new { message = "Invalid user token" });

            var result = await _userService.GetByIdAsync(userId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Logs out the current user (invalidates refresh token).
        /// </summary>
        /// <returns>Logout result</returns>
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(new { message = "Invalid user token" });

            // This would typically invalidate the refresh token
            // Implementation depends on your token management strategy
            return Ok(new { message = "Successfully logged out" });
        }

        /// <summary>
        /// Uploads a profile image for the current user.
        /// </summary>
        /// <param name="file">The image file to upload.</param>
        /// <returns>Upload result</returns>
        //[HttpPost("upload-profile-image")]
        //public async Task<IActionResult> UploadProfileImage([FromForm] IFormFile file)
        //{
        //    var userId = GetCurrentUserId();
        //    if (userId == 0)
        //        return Unauthorized(new { message = "Invalid user token" });

        //    if (file == null || file.Length == 0)
        //        return BadRequest(new { message = "No file uploaded" });

        //    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles");
        //    if (!Directory.Exists(uploadsFolder))
        //        Directory.CreateDirectory(uploadsFolder);

        //    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        //    var filePath = Path.Combine(uploadsFolder, fileName);

        //    using (var stream = new FileStream(filePath, FileMode.Create))
        //    {
        //        await file.CopyToAsync(stream);
        //    }

        //    var imageUrl = $"/images/profiles/{fileName}";
        //    var result = await _userService.UpdateProfileImageAsync(userId, imageUrl);

        //    return StatusCode(result.StatusCode, result);
        //}

        /// <summary>
        /// Gets the current user ID from claims.
        /// </summary>
        /// <returns>User ID or 0 if invalid</returns>
        private int GetCurrentUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return 0;
            return userId;
        }

        private IActionResult ValidationErrorResponse()
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.ErrorResponse("Validation failed.", 400, errors));
        }

        [HttpGet("{sellerId}/is-vip")]
        public async Task<IActionResult> CheckVipStatus(int sellerId)
        {
            var result = await _userService.IsVipSellerAsync(sellerId);
            return StatusCode(result.StatusCode, result);
        }


        [HttpGet("public-profile/{userId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicProfile(int userId)
        {
            var currentUser = GetCurrentUserId();
            var response = await _userService.GetPublicUserProfileAsync(userId, currentUser);
            return StatusCode(response.StatusCode, response);
        }

        //[HttpPut("update-profile-photo")]
        //[Authorize]
        //public async Task<IActionResult> UpdateProfilePhoto([FromBody] UpdateProfilePhotoDto dto)
        //{
        //    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        //    var response = await _userService.UpdateProfileImageAsync(userId, dto);
        //    return StatusCode(response.StatusCode, response);
        //}

        [HttpPost("upload-profile-image")]
        [Authorize] // أو حسب احتياجك
        public async Task<IActionResult> UploadProfileImage([FromForm] IFormFile imageFile)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var response = await _userService.UploadProfileImageAsync(userId, imageFile);
            return StatusCode(response.StatusCode, response);
        }
        [HttpGet("User-info/{userId}")]
        //[Authorize('Delevery')]
        public async Task<IActionResult> GetUserAddressInfo(int userId)
        {

            var response = await _userService.GetUserAddressInfo(userId);
            return StatusCode(response.StatusCode, response);
        }
    }
}