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

        // 2FA - Xác thực 2 bước & Mã dự phòng (Issue #3 / SEC-2FA-LGN-06)
        Task<ApiResponse<TwoFactorSetupResponseDto>> SetupTwoFactorAsync(Guid userId);
        Task<ApiResponse<TwoFactorEnableResponseDto>> EnableTwoFactorAsync(Guid userId, EnableTwoFactorDto dto);
        Task<ApiResponse<LoginResponseDto>> VerifyTwoFactorLoginAsync(VerifyTwoFactorLoginDto dto);
        Task<ApiResponse<bool>> DisableTwoFactorAsync(Guid userId, DisableTwoFactorDto dto);
        Task<ApiResponse<List<string>>> RegenerateBackupCodesAsync(Guid userId, RegenerateBackupCodesDto dto);
        Task<ApiResponse<int>> GetRemainingBackupCodesCountAsync(Guid userId);
    }
}
