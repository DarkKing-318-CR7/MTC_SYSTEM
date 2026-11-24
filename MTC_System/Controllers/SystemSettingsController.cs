using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MTC_System.Common;
using MTC_System.DTOs;
using MTC_System.Services;

namespace MTC_System.Controllers
{
    /// <summary>
    /// Controller Thiết lập Hệ thống Tổ chức & Cấu hình cá nhân
    /// - Thiết lập hệ thống: Chỉ hiển thị ở cấp Tổ chức cao nhất và áp dụng cho toàn bộ tổ chức (Chế độ tạo bản tin & Múi giờ).
    /// - Cấu hình cá nhân: Múi giờ và Chế độ xem bản tin (dạng bảng/lịch).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SystemSettingsController : ControllerBase
    {
        private readonly ISystemSettingService _settingService;

        public SystemSettingsController(ISystemSettingService settingService)
        {
            _settingService = settingService;
        }

        // =========================================================
        // DANH MỤC MÚI GIỜ
        // =========================================================

        /// <summary>
        /// Lấy danh sách múi giờ hỗ trợ để hiển thị trong dropdown chọn múi giờ
        /// </summary>
        [HttpGet("timezones")]
        public async Task<IActionResult> GetTimezones()
        {
            var res = await _settingService.GetAvailableTimezonesAsync();
            return Ok(res);
        }

        // =========================================================
        // THIẾT LẬP HỆ THỐNG CẤP TỔ CHỨC CAO NHẤT
        // (Chỉ hiển thị ở cấp Tổ chức cao nhất & áp dụng cho toàn bộ tổ chức)
        // =========================================================

        /// <summary>
        /// Lấy cấu hình thiết lập hệ thống theo tổ chức (Chế độ tạo bản tin & Múi giờ)
        /// Chỉ hiển thị ở cấp Tổ chức cao nhất.
        /// </summary>
        [HttpGet("organization/{organizationId}/broadcast-mode")]
        [HttpGet("organization/{organizationId}/system-setting")]
        public async Task<IActionResult> GetBroadcastSetting(
            Guid organizationId,
            [FromQuery] Guid? requestingFacilityId = null)
        {
            var scope = User.GetUserScope();
            Guid? effectiveFacilityId = requestingFacilityId ?? scope.FacilityId;

            var res = await _settingService.GetBroadcastSettingAsync(
                organizationId,
                effectiveFacilityId,
                scope.IsSystemAdministrator);

            if (!res.Success)
            {
                if (res.Message.Contains("Tổ chức cao nhất", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(403, res);
                }
                return BadRequest(res);
            }

            return Ok(res);
        }

        /// <summary>
        /// Lấy cấu hình thiết lập hệ thống của tổ chức của người dùng đang đăng nhập
        /// </summary>
        [HttpGet("system-setting")]
        public async Task<IActionResult> GetCurrentOrgSystemSetting([FromQuery] Guid? requestingFacilityId = null)
        {
            var scope = User.GetUserScope();
            if (!scope.OrganizationId.HasValue && !scope.IsSystemAdministrator)
            {
                return BadRequest(ApiResponse<OrganizationBroadcastSettingDto>.Fail("Không xác định được tổ chức của người dùng!"));
            }

            var orgId = scope.OrganizationId ?? Guid.Empty;
            Guid? effectiveFacilityId = requestingFacilityId ?? scope.FacilityId;

            var res = await _settingService.GetBroadcastSettingAsync(
                orgId,
                effectiveFacilityId,
                scope.IsSystemAdministrator);

            if (!res.Success)
            {
                if (res.Message.Contains("Tổ chức cao nhất", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(403, res);
                }
                return BadRequest(res);
            }

            return Ok(res);
        }

        /// <summary>
        /// Cập nhật thiết lập hệ thống (Chế độ tạo bản tin: Cơ sở/Thiết bị & Múi giờ)
        /// Áp dụng cho toàn bộ tổ chức. Chỉ cấp Tổ chức cao nhất mới có quyền cập nhật.
        /// </summary>
        [HttpPut("organization/{organizationId}/broadcast-mode")]
        [HttpPut("organization/{organizationId}/system-setting")]
        public async Task<IActionResult> UpdateBroadcastSetting(
            Guid organizationId,
            [FromBody] OrganizationBroadcastSettingDto dto,
            [FromQuery] Guid? requestingFacilityId = null)
        {
            var scope = User.GetUserScope();
            Guid? effectiveFacilityId = requestingFacilityId ?? scope.FacilityId;

            var res = await _settingService.UpdateBroadcastSettingAsync(
                organizationId,
                dto,
                effectiveFacilityId,
                scope.IsSystemAdministrator);

            if (!res.Success)
            {
                if (res.Message.Contains("Tổ chức cao nhất", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(403, res);
                }
                return BadRequest(res);
            }

            return Ok(res);
        }

        /// <summary>
        /// Cập nhật thiết lập hệ thống cho tổ chức của người dùng đang đăng nhập
        /// </summary>
        [HttpPut("system-setting")]
        public async Task<IActionResult> UpdateCurrentOrgSystemSetting(
            [FromBody] OrganizationBroadcastSettingDto dto,
            [FromQuery] Guid? requestingFacilityId = null)
        {
            var scope = User.GetUserScope();
            if (!scope.OrganizationId.HasValue && !scope.IsSystemAdministrator)
            {
                return BadRequest(ApiResponse<OrganizationBroadcastSettingDto>.Fail("Không xác định được tổ chức của người dùng!"));
            }

            var orgId = dto.OrganizationId != Guid.Empty
                ? dto.OrganizationId
                : (scope.OrganizationId ?? Guid.Empty);

            Guid? effectiveFacilityId = requestingFacilityId ?? scope.FacilityId;

            var res = await _settingService.UpdateBroadcastSettingAsync(
                orgId,
                dto,
                effectiveFacilityId,
                scope.IsSystemAdministrator);

            if (!res.Success)
            {
                if (res.Message.Contains("Tổ chức cao nhất", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(403, res);
                }
                return BadRequest(res);
            }

            return Ok(res);
        }

        /// <summary>
        /// Lấy toàn bộ tham số cấu hình thô của một tổ chức
        /// </summary>
        [HttpGet("organization/{organizationId}")]
        public async Task<IActionResult> GetByOrganization(Guid organizationId)
        {
            var res = await _settingService.GetByOrganizationAsync(organizationId);
            return Ok(res);
        }

        // =========================================================
        // CẤU HÌNH CÁ NHÂN NGƯỜI DÙNG (Múi giờ & Chế độ xem: Table/Calendar)
        // =========================================================

        /// <summary>
        /// Lấy cấu hình cá nhân của người dùng hiện tại đang đăng nhập
        /// </summary>
        [HttpGet("personal")]
        [HttpGet("user/me")]
        public async Task<IActionResult> GetCurrentUserPersonalSetting()
        {
            var scope = User.GetUserScope();
            if (!scope.UserId.HasValue)
            {
                return BadRequest(ApiResponse<UserPersonalSettingDto>.Fail("Không xác định được danh tính người dùng hiện tại!"));
            }

            var res = await _settingService.GetUserPersonalSettingAsync(scope.UserId.Value);
            if (!res.Success) return NotFound(res);
            return Ok(res);
        }

        /// <summary>
        /// Cập nhật cấu hình cá nhân của người dùng hiện tại đang đăng nhập
        /// </summary>
        [HttpPut("personal")]
        [HttpPut("user/me")]
        public async Task<IActionResult> UpdateCurrentUserPersonalSetting([FromBody] UpdatePersonalSettingRequestDto req)
        {
            var scope = User.GetUserScope();
            if (!scope.UserId.HasValue)
            {
                return BadRequest(ApiResponse<UserPersonalSettingDto>.Fail("Không xác định được danh tính người dùng hiện tại!"));
            }

            var currentSetting = await _settingService.GetUserPersonalSettingAsync(scope.UserId.Value);
            var targetTimezone = !string.IsNullOrWhiteSpace(req.Timezone)
                ? req.Timezone
                : (currentSetting.Data?.Timezone ?? "SE Asia Standard Time");

            var targetViewMode = !string.IsNullOrWhiteSpace(req.BroadcastViewMode)
                ? req.BroadcastViewMode
                : (currentSetting.Data?.BroadcastViewMode ?? "Table");

            var updateDto = new UserPersonalSettingDto
            {
                UserId = scope.UserId.Value,
                Timezone = targetTimezone,
                BroadcastViewMode = targetViewMode
            };

            var res = await _settingService.UpdateUserPersonalSettingAsync(updateDto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        /// <summary>
        /// Lấy cấu hình cá nhân theo UserId
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserPersonalSetting(Guid userId)
        {
            var res = await _settingService.GetUserPersonalSettingAsync(userId);
            if (!res.Success) return NotFound(res);
            return Ok(res);
        }

        /// <summary>
        /// Cập nhật cấu hình cá nhân theo DTO (chứa UserId)
        /// </summary>
        [HttpPut("user")]
        public async Task<IActionResult> UpdateUserPersonalSetting([FromBody] UserPersonalSettingDto dto)
        {
            var res = await _settingService.UpdateUserPersonalSettingAsync(dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        /// <summary>
        /// Cập nhật cấu hình cá nhân theo UserId trên URL route
        /// </summary>
        [HttpPut("user/{userId}")]
        public async Task<IActionResult> UpdateUserPersonalSettingById(Guid userId, [FromBody] UpdatePersonalSettingRequestDto req)
        {
            var currentSetting = await _settingService.GetUserPersonalSettingAsync(userId);
            if (!currentSetting.Success)
            {
                return NotFound(currentSetting);
            }

            var targetTimezone = !string.IsNullOrWhiteSpace(req.Timezone)
                ? req.Timezone
                : (currentSetting.Data?.Timezone ?? "SE Asia Standard Time");

            var targetViewMode = !string.IsNullOrWhiteSpace(req.BroadcastViewMode)
                ? req.BroadcastViewMode
                : (currentSetting.Data?.BroadcastViewMode ?? "Table");

            var updateDto = new UserPersonalSettingDto
            {
                UserId = userId,
                Timezone = targetTimezone,
                BroadcastViewMode = targetViewMode
            };

            var res = await _settingService.UpdateUserPersonalSettingAsync(updateDto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }
    }
}
