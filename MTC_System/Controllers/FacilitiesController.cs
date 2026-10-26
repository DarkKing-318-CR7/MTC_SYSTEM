using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MTC_System.DTOs;
using MTC_System.Services;

namespace MTC_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacilitiesController : ControllerBase
    {
        private readonly IFacilityService _facilityService;

        public FacilitiesController(IFacilityService facilityService)
        {
            _facilityService = facilityService;
        }

        // Lấy danh sách cây địa bàn phân cấp: Tỉnh -> Huyện -> Xã
        [HttpGet("tree/{organizationId}")]
        public async Task<IActionResult> GetTree(Guid organizationId)
        {
            var res = await _facilityService.GetTreeAsync(organizationId);
            return Ok(res);
        }

        // Lấy danh sách phẳng theo tổ chức
        [HttpGet("organization/{organizationId}")]
        public async Task<IActionResult> GetByOrganization(Guid organizationId)
        {
            var res = await _facilityService.GetByOrganizationAsync(organizationId);
            return Ok(res);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var res = await _facilityService.GetByIdAsync(id);
            if (!res.Success) return NotFound(res);
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFacilityDto dto)
        {
            var res = await _facilityService.CreateAsync(dto);
            if (!res.Success) return BadRequest(res);
            return CreatedAtAction(nameof(GetById), new { id = res.Data!.Id }, res);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFacilityDto dto)
        {
            var res = await _facilityService.UpdateAsync(id, dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var res = await _facilityService.DeleteAsync(id);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }
    }
}
