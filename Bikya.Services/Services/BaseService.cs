using Bikya.Data.Models;
using Bikya.Services.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bikya.Services.Services
{
    /// <summary>
    /// Base service class providing common functionality for all services.
    /// </summary>
    public abstract class BaseService
    {
        protected readonly ILogger _logger;
        protected readonly UserManager<ApplicationUser> _userManager;

        protected BaseService(ILogger logger, UserManager<ApplicationUser> userManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        #region User Validation

        /// <summary>
        /// Checks if a user exists by ID.
        /// </summary>
        /// <param name="userId">The user ID to check.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the user exists, otherwise false.</returns>
        public async Task<bool> UserExistsAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _userManager.Users.AnyAsync(u => u.Id == userId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {UserId} exists", userId);
                throw;
            }
        }

        /// <summary>
        /// Checks if a user has admin role.
        /// </summary>
        /// <param name="userId">The user ID to check.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the user is an admin, otherwise false.</returns>
        public async Task<bool> IsAdminAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null) return false;

                var roles = await _userManager.GetRolesAsync(user);
                return roles.Contains("Admin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {UserId} is admin", userId);
                throw;
            }
        }

        /// <summary>
        /// Validates that a user exists and throws NotFoundException if not.
        /// </summary>
        /// <param name="userId">The user ID to validate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected async Task ValidateUserExistsAsync(int userId, CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
                throw new ValidationException("Valid user ID is required.");

            var userExists = await UserExistsAsync(userId, cancellationToken);
            if (!userExists)
                throw new NotFoundException("User", userId);
        }

        /// <summary>
        /// Validates that a user has permission to access a resource.
        /// </summary>
        /// <param name="userId">The user ID making the request.</param>
        /// <param name="resourceUserId">The user ID that owns the resource.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected async Task ValidateUserPermissionAsync(int userId, int resourceUserId, CancellationToken cancellationToken = default)
        {
            if (userId != resourceUserId)
            {
                var isAdmin = await IsAdminAsync(userId, cancellationToken);
                if (!isAdmin)
                    throw new UnauthorizedException($"You do not have permission to access this resource.");
            }
        }

        #endregion

        #region Common Validation

        /// <summary>
        /// Validates that an entity is not null.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entity">The entity to check.</param>
        /// <param name="entityName">The name of the entity.</param>
        /// <param name="id">The ID of the entity.</param>
        protected void ValidateEntityNotNull<T>(T entity, string entityName, object id) where T : class
        {
            if (entity == null)
                throw new NotFoundException(entityName, id);
        }

        /// <summary>
        /// Validates that a string is not null or whitespace.
        /// </summary>
        /// <param name="value">The string value to check.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        protected void ValidateRequiredString(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ValidationException($"{parameterName} is required.");
        }

        /// <summary>
        /// Validates that an ID is positive.
        /// </summary>
        /// <param name="id">The ID to check.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        protected void ValidatePositiveId(int id, string parameterName)
        {
            if (id <= 0)
                throw new ValidationException($"Valid {parameterName} is required.");
        }

        /// <summary>
        /// Validates that a price is non-negative.
        /// </summary>
        /// <param name="price">The price to check.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        protected void ValidateNonNegativePrice(decimal price, string parameterName)
        {
            if (price < 0)
                throw new ValidationException($"{parameterName} cannot be negative.");
        }

        #endregion

        #region Common Operations

        /// <summary>
        /// Trims and validates a string value.
        /// </summary>
        /// <param name="value">The string value to trim and validate.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <returns>The trimmed string.</returns>
        protected string TrimAndValidateString(string value, string parameterName)
        {
            ValidateRequiredString(value, parameterName);
            return value.Trim();
        }

        /// <summary>
        /// Trims a nullable string value.
        /// </summary>
        /// <param name="value">The nullable string value to trim.</param>
        /// <returns>The trimmed string or null.</returns>
        protected string? TrimNullableString(string? value)
        {
            return value?.Trim();
        }

        /// <summary>
        /// Logs an information message with structured logging.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Arguments for the message.</param>
        protected void LogInformation(string message, params object[] args)
        {
            _logger.LogInformation(message, args);
        }

        /// <summary>
        /// Logs a warning message with structured logging.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Arguments for the message.</param>
        protected void LogWarning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
        }

        /// <summary>
        /// Logs an error message with structured logging.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Arguments for the message.</param>
        protected void LogError(Exception ex, string message, params object[] args)
        {
            _logger.LogError(ex, message, args);
        }

        #endregion
    }
} 