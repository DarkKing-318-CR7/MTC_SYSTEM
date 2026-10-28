using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MTC_System.DTOs;
using MTC_System.Services;

namespace MTC_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrganizationsController : ControllerBase
    {
        private readonly IOrganizationService _orgService;

        public OrganizationsController(IOrganizationService orgService)
        {
            _orgService = orgService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var res = await _orgService.GetAllAsync();
            return Ok(res);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var res = await _orgService.GetByIdAsync(id);
            if (!res.Success) return NotFound(res);
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrganizationDto dto)
        {
            var res = await _orgService.CreateAsync(dto);
            if (!res.Success) return BadRequest(res);
            return CreatedAtAction(nameof(GetById), new { id = res.Data!.Id }, res);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrganizationDto dto)
        {
            var res = await _orgService.UpdateAsync(id, dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var res = await _orgService.DeleteAsync(id);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }
    }
}
