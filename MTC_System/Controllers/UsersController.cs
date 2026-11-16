using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTC_System.Common;
using MTC_System.DTOs;
using MTC_System.Services.Interfaces;

namespace MTC_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// 1. Lấy danh sách Người dùng có phân trang và bộ lọc (Search, Tổ chức, Cơ sở, Vai trò, Trạng thái)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] UserFilterDto filter)
        {
            var result = await _userService.GetPagedAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// 2. Lấy thông tin chi tiết một Người dùng theo Id
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _userService.GetByIdAsync(id);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// 3. Tạo tài khoản Người dùng mới (Gán Tổ chức / Cơ sở và phân quyền Vai trò)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserCreateDto dto)
        {
            var result = await _userService.CreateAsync(dto);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
        }

        /// <summary>
        /// 4. Cập nhật thông tin Người dùng (Họ tên, SĐT, Email, Cơ sở, Vai trò, Trạng thái)
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UserUpdateDto dto)
        {
            var result = await _userService.UpdateAsync(id, dto);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// 5. Khóa hoặc Mở khóa tài khoản Người dùng
        /// </summary>
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateUserStatusDto dto)
        {
            var result = await _userService.UpdateStatusAsync(id, dto.IsActive);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// 6. Đặt lại mật khẩu cho Người dùng (Thu hồi mọi phiên đăng nhập cũ)
        /// </summary>
        [HttpPost("{id:guid}/reset-password")]
        public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordDto dto)
        {
            var result = await _userService.ResetPasswordAsync(id, dto);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// 7. Xóa người dùng (Xóa mềm và thu hồi quyền truy cập)
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _userService.DeleteAsync(id);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
