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
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        /// <summary>
        /// 1. Lấy danh sách Vai trò (Roles) trong hệ thống hoặc theo Tổ chức
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] Guid? organizationId = null)
        {
            var result = await _roleService.GetAllRolesAsync(organizationId);
            return Ok(result);
        }

        /// <summary>
        /// 2. Lấy thông tin chi tiết một Vai trò kèm danh sách quyền đã phân
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _roleService.GetRoleByIdAsync(id);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// 3. Tạo mới một Vai trò tùy biến cho Tổ chức
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RoleCreateDto dto)
        {
            var result = await _roleService.CreateRoleAsync(dto);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
        }

        /// <summary>
        /// 4. Lấy danh mục toàn bộ Quyền chức năng (Permissions) trong hệ thống
        /// </summary>
        [HttpGet("permissions")]
        public async Task<IActionResult> GetAllPermissions()
        {
            var result = await _roleService.GetAllPermissionsAsync();
            return Ok(result);
        }

        /// <summary>
        /// 5. Cập nhật Ma trận Phân quyền cho Vai trò (Role Matrix)
        /// </summary>
        [HttpPost("{id:guid}/permissions")]
        public async Task<IActionResult> AssignPermissions(Guid id, [FromBody] AssignPermissionsDto dto)
        {
            var result = await _roleService.AssignPermissionsToRoleAsync(id, dto);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
