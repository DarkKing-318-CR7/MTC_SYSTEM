 using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MTC_System.Common;
using MTC_System.DTOs;

namespace MTC_System.Services
{
    public interface ISystemSettingService
    {
        /// <summary>
        /// Lấy toàn bộ cấu hình thuộc một tổ chức
        /// </summary>
        Task<ApiResponse<List<SystemSettingResponseDto>>> GetByOrganizationAsync(Guid organizationId);

        /// <summary>
        /// Lấy thiết lập hệ thống (Chế độ tạo bản tin & Múi giờ).
        /// Chỉ hiển thị ở cấp Tổ chức cao nhất và áp dụng cho toàn bộ tổ chức.
        /// </summary>
        Task<ApiResponse<OrganizationBroadcastSettingDto>> GetBroadcastSettingAsync(
            Guid organizationId,
            Guid? requestingFacilityId = null,
            bool isSystemAdmin = false);

        /// <summary>
        /// Cập nhật thiết lập hệ thống (Chế độ tạo bản tin & Múi giờ).
        /// Chỉ cho phép cập nhật ở cấp Tổ chức cao nhất và áp dụng cho toàn bộ tổ chức.
        /// </summary>
        Task<ApiResponse<OrganizationBroadcastSettingDto>> UpdateBroadcastSettingAsync(
            Guid organizationId,
            OrganizationBroadcastSettingDto dto,
            Guid? requestingFacilityId = null,
            bool isSystemAdmin = false);

        /// <summary>
        /// Lấy cấu hình cá nhân của người dùng (Múi giờ & Chế độ xem bản tin: dạng bảng/lịch)
        /// </summary>
        Task<ApiResponse<UserPersonalSettingDto>> GetUserPersonalSettingAsync(Guid userId);

        /// <summary>
        /// Cập nhật cấu hình cá nhân của người dùng
        /// </summary>
        Task<ApiResponse<UserPersonalSettingDto>> UpdateUserPersonalSettingAsync(UserPersonalSettingDto dto);

        /// <summary>
        /// Lấy danh sách múi giờ hỗ trợ
        /// </summary>
        Task<ApiResponse<List<TimezoneItemDto>>> GetAvailableTimezonesAsync();
    }
}
