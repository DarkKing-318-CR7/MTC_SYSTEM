using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTC_System.Common;
using MTC_System.DTOs;
using MTC_System.Services;
using MTC_System.Services.Interfaces;

namespace MTC_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// 1. Đăng nhập hệ thống (Cấp Access Token 15 phút & Refresh Token 7 ngày)
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// 2. Cấp lại Access Token mới bằng Refresh Token
        /// </summary>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
        {
            var result = await _authService.RefreshTokenAsync(dto);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// 3. Đăng xuất hệ thống (Thu hồi Refresh Token)
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromQuery] string? token = null)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse<bool>.Fail("Không xác định được danh tính người dùng."));
            }

            var result = await _authService.LogoutAsync(userId.Value, token ?? string.Empty);
            return Ok(result);
        }

        /// <summary>
        /// 4. Đổi mật khẩu cá nhân (Kiểm tra độ phức tạp theo chuẩn SRS: 8 ký tự, hoa, thường, số, ký tự đặc biệt)
        /// </summary>
        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse<bool>.Fail("Vui lòng đăng nhập trước khi thực hiện."));
            }

            var result = await _authService.ChangePasswordAsync(userId.Value, dto);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// 5. Lấy thông tin tài khoản đang đăng nhập
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUserProfile()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse<UserAuthInfoDto>.Fail("Không tìm thấy phiên đăng nhập."));
            }

            var result = await _authService.GetProfileAsync(userId.Value);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// 6. Cập nhật thông tin cá nhân (Họ tên, SĐT, Email)
        /// </summary>
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse<UserAuthInfoDto>.Fail("Vui lòng đăng nhập."));
            }

            var result = await _authService.UpdateProfileAsync(userId.Value, dto);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        #region Helper
        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                           ?? User.FindFirstValue("sub");
            if (Guid.TryParse(userIdClaim, out var id))
            {
                return id;
            }
            return null;
        }
        #endregion
    }
}
