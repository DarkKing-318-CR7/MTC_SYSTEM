using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MTC_System.DTOs;
using MTC_System.Services;

namespace MTC_System.Controllers
{
    /// <summary>
    /// [Dev 3] Controller Thiết lập Hệ thống Tổ chức & Cấu hình cá nhân (Issue #12 - USR-SET)
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

        /// <summary>
        /// Lấy toàn bộ tham số cấu hình của một tổ chức
        /// </summary>
        [HttpGet("organization/{organizationId}")]
        public async Task<IActionResult> GetByOrganization(Guid organizationId)
        {
            var res = await _settingService.GetByOrganizationAsync(organizationId);
            return Ok(res);
        }

        /// <summary>
        /// Lấy cấu hình chế độ phát (Cơ sở vs Thiết bị) và múi giờ của tổ chức
        /// </summary>
        [HttpGet("organization/{organizationId}/broadcast-mode")]
        public async Task<IActionResult> GetBroadcastSetting(Guid organizationId)
        {
            var res = await _settingService.GetBroadcastSettingAsync(organizationId);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        /// <summary>
        /// Cập nhật chế độ phát và múi giờ của tổ chức (Áp dụng cấp Tổ chức cao nhất)
        /// </summary>
        [HttpPut("organization/{organizationId}/broadcast-mode")]
        public async Task<IActionResult> UpdateBroadcastSetting(Guid organizationId, [FromBody] OrganizationBroadcastSettingDto dto)
        {
            var res = await _settingService.UpdateBroadcastSettingAsync(organizationId, dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        /// <summary>
        /// Lấy cấu hình cá nhân của người dùng (Múi giờ & Chế độ xem Table/Calendar)
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserPersonalSetting(Guid userId)
        {
            var res = await _settingService.GetUserPersonalSettingAsync(userId);
            if (!res.Success) return NotFound(res);
            return Ok(res);
        }

        /// <summary>
        /// Cập nhật cấu hình cá nhân của người dùng
        /// </summary>
        [HttpPut("user")]
        public async Task<IActionResult> UpdateUserPersonalSetting([FromBody] UserPersonalSettingDto dto)
        {
            var res = await _settingService.UpdateUserPersonalSettingAsync(dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }
    }
}
