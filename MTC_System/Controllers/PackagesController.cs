using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MTC_System.DTOs.Packages;
using MTC_System.Services;

namespace MTC_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PackagesController : ControllerBase
    {
        private readonly IPackageService _packageService;

        public PackagesController(IPackageService packageService)
        {
            _packageService = packageService;
        }

        // GET: api/Packages
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var res = await _packageService.GetAllAsync();
            return Ok(res);
        }

        // GET: api/Packages/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var res = await _packageService.GetByIdAsync(id);

            if (!res.Success)
                return NotFound(res);

            return Ok(res);
        }

        // POST: api/Packages
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] PackageCreateDto dto)
        {
            var res = await _packageService.CreateAsync(dto);

            if (!res.Success)
                return BadRequest(res);

            return CreatedAtAction(
                nameof(GetById),
                new { id = res.Data!.Id },
                res);
        }

        // PUT: api/Packages/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] PackageUpdateDto dto)
        {
            var res = await _packageService.UpdateAsync(id, dto);

            if (!res.Success)
                return BadRequest(res);

            return Ok(res);
        }

        // DELETE: api/Packages/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var res = await _packageService.DeleteAsync(id);

            if (!res.Success)
                return BadRequest(res);

            return Ok(res);
        }
    }
}