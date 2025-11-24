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

    public interface IAuthService
    {
        Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto);
        Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto);

        Task<ApiResponse<UserProfileDto>> GetProfileAsync(int userId);
        Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto dto);
        Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordDto dto);
        // Email verification
        Task<ApiResponse<string>> SendVerificationEmailAsync(string email);
        Task<ApiResponse<AuthResponseDto>> VerifyEmailAsync(string token, string email);

    }

}
