using System;
using System.Threading.Tasks;
using MTC_System.Common;
using MTC_System.DTOs;

namespace MTC_System.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto dto);
        Task<ApiResponse<LoginResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto);
        Task<ApiResponse<bool>> LogoutAsync(Guid userId, string token);
        Task<ApiResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
        Task<ApiResponse<UserAuthInfoDto>> GetProfileAsync(Guid userId);
        Task<ApiResponse<UserAuthInfoDto>> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
    }
}
