using Bikya.Data.Models;
using Bikya.Data.Response;
using Bikya.DTOs.AuthDTOs;
using Bikya.DTOs.UserDTOs;
using Bikya.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Web;

namespace Bikya.Services.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;
        private readonly IEmailSender _emailSender;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            IJwtService jwtService,
            ILogger<AuthService> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
            _logger = logger;
            _emailSender = emailSender;
        }

        /// <summary>
        /// Registers a new user (regular user or admin).
        /// </summary>
        public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto)
        {
            try
            {
                _logger.LogInformation("Starting registration for email: {Email}", dto.Email);

                // Enhanced validation
                if (string.IsNullOrWhiteSpace(dto.Email))
                    return ApiResponse<AuthResponseDto>.ErrorResponse("Email is required.", 400);
                if (string.IsNullOrWhiteSpace(dto.Password))
                    return ApiResponse<AuthResponseDto>.ErrorResponse("Password is required.", 400);
                if (dto.Password.Length < 6)
                    return ApiResponse<AuthResponseDto>.ErrorResponse("Password must be at least 6 characters.", 400);
                if (dto.Password != dto.ConfirmPassword)
                    return ApiResponse<AuthResponseDto>.ErrorResponse("Password and confirmation do not match.", 400);
                if (!IsValidEmail(dto.Email))
                    return ApiResponse<AuthResponseDto>.ErrorResponse("Invalid email format.", 400);
                if (dto.UserType != "User" && dto.UserType != "Admin")
                    return ApiResponse<AuthResponseDto>.ErrorResponse("User type must be 'User' or 'Admin'.", 400);

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(dto.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Registration failed: Email {Email} already exists", dto.Email);
                    return ApiResponse<AuthResponseDto>.ErrorResponse("User already exists.", 400);
                }

                // Create new user
                var user = new ApplicationUser
                {
                    UserName = dto.Email,
                    Email = dto.Email,
                    FullName = dto.FullName,
                    PhoneNumber = dto.PhoneNumber,
                    Address = dto.Address,
                    City = dto.City,
                    PostalCode = dto.PostalCode,
                    EmailConfirmed = false, // Changed to false to require email verification
                    IsVerified = false
                };

                var result = await _userManager.CreateAsync(user, dto.Password);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    _logger.LogError("User creation failed for {Email}: {Errors}", dto.Email, string.Join(", ", errors));
                    return ApiResponse<AuthResponseDto>.ErrorResponse("Registration failed.", 400, errors);
                }

                // Assign role
                string roleToAssign = dto.UserType == "Admin" ? "Admin" : "User";
                if (!await _roleManager.RoleExistsAsync(roleToAssign))
                {
                    await _roleManager.CreateAsync(new ApplicationRole { Name = roleToAssign });
                }

                var roleResult = await _userManager.AddToRoleAsync(user, roleToAssign);
                if (!roleResult.Succeeded)
                {
                    var errors = roleResult.Errors.Select(e => e.Description).ToList();
                    _logger.LogError("Role assignment failed for {Email}: {Errors}", dto.Email, string.Join(", ", errors));
                    return ApiResponse<AuthResponseDto>.ErrorResponse("Role assignment failed.", 400, errors);
                }

                // Send verification email
                try
                {
                    var verificationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var verificationUrl = $"http://localhost:4200/verify-email?token={Uri.EscapeDataString(verificationToken)}&email={Uri.EscapeDataString(user.Email)}";
                    var subject = "Welcome to Bikya - Verify Your Email";
                    var body = $@"
                        <html>
                        <body>
                            <h2>Welcome to Bikya!</h2>
                            <p>Hello {user.FullName},</p>
                            <p>Thank you for registering with Bikya. Please verify your email address by clicking the link below:</p>
                            <p><a href='{verificationUrl}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Verify Email</a></p>
                            <p>Or copy and paste this link in your browser:</p>
                            <p>{verificationUrl}</p>
                            <p>This link will expire in 24 hours.</p>
                            <p>Best regards,<br>The Bikya Team</p>
                        </body>
                        </html>";

                    await _emailSender.SendEmailAsync(user.Email, subject, body);
                    _logger.LogInformation("Verification email sent to {Email} during registration", user.Email);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send verification email to {Email} during registration", user.Email);
                    // If email sending fails, delete the user and return error
                    await _userManager.DeleteAsync(user);
                    return ApiResponse<AuthResponseDto>.ErrorResponse("Failed to send verification email. Please try again.", 500);
                }

                // Return success without token - user must verify email first
                var successMessage = roleToAssign == "Admin" ? "Admin registration initiated. Please check your email and verify your account before logging in." : "Registration initiated. Please check your email and verify your account before logging in.";
                _logger.LogInformation("User {Email} registration initiated as {Role} - awaiting email verification", dto.Email, roleToAssign);

                return ApiResponse<AuthResponseDto>.SuccessResponse(null, successMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for {Email}", dto.Email);
                return ApiResponse<AuthResponseDto>.ErrorResponse("An unexpected error occurred.", 500);
            }
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token.
        /// </summary>
        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto)
        {
            try
            {
                _logger.LogInformation("Login attempt for email: {Email}", dto.Email);

                // Enhanced validation
                if (string.IsNullOrWhiteSpace(dto.Email))
                    return ApiResponse<AuthResponseDto>.ErrorResponse("Email is required.", 400);
                if (string.IsNullOrWhiteSpace(dto.Password))
                    return ApiResponse<AuthResponseDto>.ErrorResponse("Password is required.", 400);
                if (!IsValidEmail(dto.Email))
                    return ApiResponse<AuthResponseDto>.ErrorResponse("Invalid email format.", 400);

                var user = await _userManager.FindByEmailAsync(dto.Email);
                if (user == null)
                {
                    _logger.LogWarning("Login failed: User not found for email {Email}", dto.Email);
                    return ApiResponse<AuthResponseDto>.ErrorResponse("Invalid login credentials.", 401);
                }

                // Check if user is deleted
                if (user.IsDeleted)
                {
                    _logger.LogWarning("Login failed: Deleted user {Email} attempted to login", dto.Email);
                    return ApiResponse<AuthResponseDto>.ErrorResponse("Account is deleted.", 401);
                }

                // Check if user is locked
                if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
                {
                    _logger.LogWarning("Login failed: Locked user {Email} attempted to login", dto.Email);
                    return ApiResponse<AuthResponseDto>.ErrorResponse("Account is locked.", 401);
                }

                // Check if email is confirmed
                if (!user.EmailConfirmed)
                {
                    _logger.LogWarning("Login failed: Unverified email for user {Email}", dto.Email);
                    return ApiResponse<AuthResponseDto>.ErrorResponse("Please verify your email address before logging in. Check your inbox for the verification email.", 401);
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Login failed: Invalid password for user {Email}", dto.Email);
                    return ApiResponse<AuthResponseDto>.ErrorResponse("Invalid login credentials.", 401);
                }

                // Generate token
                var token = await _jwtService.GenerateAccessTokenAsync(user);
                var roles = await _userManager.GetRolesAsync(user);
                var response = new AuthResponseDto
                {
                    Token = token,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber=user.PhoneNumber,
                    Address= user.Address,
                    City = user.City,
                    PostalCode = user.PostalCode,
                    UserId = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Roles = roles.ToList(),
                    Expiration = DateTime.UtcNow.AddMinutes(60) // Default 60 minutes
                };

                _logger.LogInformation("User {Email} logged in successfully", dto.Email);
                return ApiResponse<AuthResponseDto>.SuccessResponse(response, "Login successful.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for {Email}", dto.Email);
                return ApiResponse<AuthResponseDto>.ErrorResponse("An unexpected error occurred.", 500);
            }
        }

        /// <summary>
        /// Gets the profile of a user by user ID.
        /// </summary>
        public async Task<ApiResponse<UserProfileDto>> GetProfileAsync(int userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    _logger.LogWarning("Profile request failed: User {UserId} not found", userId);
                    return ApiResponse<UserProfileDto>.ErrorResponse("User not found.", 404);
                }

                var roles = await _userManager.GetRolesAsync(user);

                var profile = new UserProfileDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    UserName = user.UserName,
                    Address = user.Address,
                    City=user.City,
                    PostalCode=user.PostalCode,
                    ProfileImageUrl = user.ProfileImageUrl,
                    IsVerified = user.IsVerified,
                    PhoneNumber = user.PhoneNumber,
                    SellerRating = user.SellerRating,
                    Roles = roles.ToList()
                };

                return ApiResponse<UserProfileDto>.SuccessResponse(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile for user {UserId}", userId);
                return ApiResponse<UserProfileDto>.ErrorResponse("An unexpected error occurred.", 500);
            }
        }

        /// <summary>
        /// Refreshes the JWT token using a refresh token.
        /// </summary>
        public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Token))
                {
                    return ApiResponse<AuthResponseDto>.ErrorResponse("Invalid refresh attempt.", 400);
                }

                var user = await _userManager.FindByEmailAsync(dto.Email);
                if (user == null)
                {
                    _logger.LogWarning("Refresh token failed: User not found for email {Email}", dto.Email);
                    return ApiResponse<AuthResponseDto>.ErrorResponse("Invalid refresh attempt.", 401);
                }

                // Validate the refresh token (in a real implementation, you'd store and validate refresh tokens)
                var principal = _jwtService.GetPrincipalFromExpiredToken(dto.Token);
                if (principal == null)
                {
                    _logger.LogWarning("Refresh token failed: Invalid token for user {Email}", dto.Email);
                    return ApiResponse<AuthResponseDto>.ErrorResponse("Invalid refresh token.", 401);
                }

                var token = await _jwtService.GenerateAccessTokenAsync(user);
                var roles = await _userManager.GetRolesAsync(user);

                var response = new AuthResponseDto
                {
                    Token = token,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Address,
                    City = user.City,
                    PostalCode = user.PostalCode,
                    UserName = user.UserName,
                    UserId = user.Id,
                    Roles = roles.ToList(),
                    Expiration = DateTime.UtcNow.AddMinutes(60)
                };

                _logger.LogInformation("Token refreshed successfully for user {Email}", dto.Email);
                return ApiResponse<AuthResponseDto>.SuccessResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token for {Email}", dto.Email);
                return ApiResponse<AuthResponseDto>.ErrorResponse("An unexpected error occurred.", 500);
            }
        }

        /// <summary>
        /// Initiates the forgot password process for a user.
        /// </summary>
        //public async Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordDto dto)
        //{
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(dto.Email))
        //            return ApiResponse<string>.ErrorResponse("Email is required.", 400);

        //        if (!IsValidEmail(dto.Email))
        //            return ApiResponse<string>.ErrorResponse("Invalid email format.", 400);

        //        var user = await _userManager.FindByEmailAsync(dto.Email);
        //        if (user == null)
        //        {
        //            // Don't reveal if email exists or not for security
        //            _logger.LogInformation("Password reset requested for non-existent email: {Email}", dto.Email);
        //            return ApiResponse<string>.SuccessResponse("If the email exists, a password reset link will be sent.");
        //        }

        //        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        //        var resetUrl = $"http://localhost:4200/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(dto.Email)}";

        //        // In a real implementation, you would send this via email
        //        _logger.LogInformation("Password reset link generated for {Email}: {ResetUrl}", dto.Email, resetUrl);

        //        return ApiResponse<string>.SuccessResponse("Password reset link sent.");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error in forgot password for {Email}", dto.Email);
        //        return ApiResponse<string>.ErrorResponse("An unexpected error occurred.", 500);
        //    }
        //}
        public async Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Email))
                    return ApiResponse<string>.ErrorResponse("Email is required.", 400);

                if (!IsValidEmail(dto.Email))
                    return ApiResponse<string>.ErrorResponse("Invalid email format.", 400);

                var user = await _userManager.FindByEmailAsync(dto.Email);
                if (user == null)
                {
                    _logger.LogInformation("Password reset requested for non-existent email: {Email}", dto.Email);
                    return ApiResponse<string>.SuccessResponse("If the email exists, a password reset link will be sent.");
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetUrl = $"http://localhost:4200/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(dto.Email)}";

                // أرسل الإيميل فعليًا هنا
                await _emailSender.SendEmailAsync(
                    user.Email,
                    "Reset your password",
                    $"Click here to reset: <a href='{resetUrl}'>Reset Password</a>"
                );
                _logger.LogInformation("Password reset link generated and sent for {Email}: {ResetUrl}", dto.Email, resetUrl);

                return ApiResponse<string>.SuccessResponse("Password reset link sent.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in forgot password for {Email}", dto.Email);
                return ApiResponse<string>.ErrorResponse("An unexpected error occurred.", 500);
            }
        }

        /// <summary>
        /// Resets the user's password using a reset token.
        /// </summary>
        public async Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.NewPassword))
                    return ApiResponse<string>.ErrorResponse("All fields are required.", 400);

                if (dto.NewPassword.Length < 6)
                    return ApiResponse<string>.ErrorResponse("New password must be at least 6 characters.", 400);

                var user = await _userManager.FindByEmailAsync(dto.Email);
                if (user == null)
                    return ApiResponse<string>.ErrorResponse("Invalid email.", 400);

                var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    _logger.LogError("Password reset failed for {Email}: {Errors}", dto.Email, string.Join(", ", errors));
                    return ApiResponse<string>.ErrorResponse("Password reset failed.", 400, errors);
                }

                _logger.LogInformation("Password reset successful for {Email}", dto.Email);
                return ApiResponse<string>.SuccessResponse("Password reset successful.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in reset password for {Email}", dto.Email);
                return ApiResponse<string>.ErrorResponse("An unexpected error occurred.", 500);
            }
        }

        /// <summary>
        /// Sends a verification email to the specified user email.
        /// </summary>
        public async Task<ApiResponse<string>> SendVerificationEmailAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                    return ApiResponse<string>.ErrorResponse("User not found.", 404);

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = HttpUtility.UrlEncode(token);

                var verificationLink = $"https://localhost:65162/api/Identity/Auth/verify-email?token={encodedToken}&email={email}";

                var body = $@"
            <h1>Hi {user.FullName},</h1>
            <p>Please click the link below to verify your email:</p>
            <a href='{verificationLink}'>Verify Email</a>";

                await _emailSender.SendEmailAsync(email, "Bikya Email Verification", body);

                return ApiResponse<string>.SuccessResponse("Verification email sent.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending verification email to {Email}", email);
                return ApiResponse<string>.ErrorResponse("Failed to send verification email. Please try again.", 500, new List<string> { ex.Message, ex.InnerException?.Message ?? "" });
            }
        }

        /// <summary>
        /// Verifies a user's email using the provided token and email.
        /// </summary>
        public async Task<ApiResponse<AuthResponseDto>> VerifyEmailAsync(string token, string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
                    return ApiResponse<AuthResponseDto>.ErrorResponse("Token and email are required for verification.", 400);

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                    return ApiResponse<AuthResponseDto>.ErrorResponse("User not found.", 404);

                if (user.EmailConfirmed)
                {
                    // If already verified, generate token and return
                    var tokenResponse = await _jwtService.GenerateAccessTokenAsync(user);
                    var roles = await _userManager.GetRolesAsync(user);
                    var response = new AuthResponseDto
                    {
                        Token = tokenResponse,
                        FullName = user.FullName,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        Address = user.Address,
                        City = user.City,
                        PostalCode = user.PostalCode,
                        UserId = user.Id,
                        UserName = user.UserName ?? string.Empty,
                        Roles = roles.ToList(),
                        Expiration = DateTime.UtcNow.AddMinutes(60)
                    };
                    return ApiResponse<AuthResponseDto>.SuccessResponse(response, "Email is already verified. Welcome back!");
                }

                var result = await _userManager.ConfirmEmailAsync(user, token);
                if (result.Succeeded)
                {
                    // Generate token after successful verification
                    var tokenResponse = await _jwtService.GenerateAccessTokenAsync(user);
                    var roles = await _userManager.GetRolesAsync(user);
                    var response = new AuthResponseDto
                    {
                        Token = tokenResponse,
                        FullName = user.FullName,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        Address = user.Address,
                        City = user.City,
                        PostalCode = user.PostalCode,
                        UserId = user.Id,
                        UserName = user.UserName ?? string.Empty,
                        Roles = roles.ToList(),
                        Expiration = DateTime.UtcNow.AddMinutes(60)
                    };

                    _logger.LogInformation("Email verified successfully for {Email}", email);
                    return ApiResponse<AuthResponseDto>.SuccessResponse(response, "Email verified successfully. Welcome to Bikya!");
                }

                var errors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogError("Email verification failed for {Email}: {Errors}", email, string.Join(", ", errors));
                return ApiResponse<AuthResponseDto>.ErrorResponse("Email verification failed.", 400, errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during email verification for {Email}", email);
                return ApiResponse<AuthResponseDto>.ErrorResponse("An unexpected error occurred during email verification.", 500);
            }
        }

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
    }
}
