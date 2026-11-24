using System;
using System.Threading.Tasks;
using MTC_System.Common;
using MTC_System.DTOs;

namespace MTC_System.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApiResponse<PagedResult<UserResponseDto>>> GetPagedAsync(UserFilterDto filter);
        Task<ApiResponse<UserResponseDto>> GetByIdAsync(Guid id);
        Task<ApiResponse<UserResponseDto>> CreateAsync(UserCreateDto dto);
        Task<ApiResponse<UserResponseDto>> UpdateAsync(Guid id, UserUpdateDto dto);
        Task<ApiResponse<bool>> UpdateStatusAsync(Guid id, bool isActive);
        Task<ApiResponse<bool>> ResetPasswordAsync(Guid id, ResetPasswordDto dto);
        Task<ApiResponse<bool>> DeleteAsync(Guid id);
    }
}
