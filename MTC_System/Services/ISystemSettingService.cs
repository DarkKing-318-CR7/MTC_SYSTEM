using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MTC_System.Common;
using MTC_System.DTOs;

namespace MTC_System.Services
{
    public interface ISystemSettingService
    {
        Task<ApiResponse<List<SystemSettingResponseDto>>> GetByOrganizationAsync(Guid organizationId);

        Task<ApiResponse<OrganizationBroadcastSettingDto>> GetBroadcastSettingAsync(Guid organizationId);

        Task<ApiResponse<OrganizationBroadcastSettingDto>> UpdateBroadcastSettingAsync(Guid organizationId, OrganizationBroadcastSettingDto dto);

        Task<ApiResponse<UserPersonalSettingDto>> GetUserPersonalSettingAsync(Guid userId);

        Task<ApiResponse<UserPersonalSettingDto>> UpdateUserPersonalSettingAsync(UserPersonalSettingDto dto);
    }
}
