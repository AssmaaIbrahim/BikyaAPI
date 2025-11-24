using Bikya.Data.Models;
using System.Security.Claims;

namespace Bikya.Services.Interfaces
{
    public interface IJwtService
    {
        Task<string> GenerateAccessTokenAsync(ApplicationUser user);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
        bool ValidateToken(string token);
        Task<ApplicationUser?> GetUserFromTokenAsync(string token);
    }
} 