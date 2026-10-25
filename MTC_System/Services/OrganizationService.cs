using Microsoft.EntityFrameworkCore;
using MTC_System.Common;
using MTC_System.Data;
using MTC_System.DTOs;
using MTC_System.Models;

namespace MTC_System.Services
{
    public class OrganizationService : IOrganizationService
    {
        private readonly MyDbContext _context;

        public OrganizationService(MyDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<List<OrganizationResponseDto>>> GetAllAsync()
        {
            var list = await _context.Organizations
                .Where(o => !o.IsDeleted)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrganizationResponseDto
                {
                    Id = o.Id,
                    OrganizationCode = o.OrganizationCode,
                    OrganizationName = o.OrganizationName,
                    TaxCode = o.TaxCode,
                    Address = o.Address,
                    Status = o.Status,
                    EffectiveDate = o.EffectiveDate,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();

            return ApiResponse<List<OrganizationResponseDto>>.Ok(list);
        }

        public async Task<ApiResponse<OrganizationResponseDto>> GetByIdAsync(Guid id)
        {
            var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
            if (org == null)
                return ApiResponse<OrganizationResponseDto>.Fail("Không tìm thấy tổ chức");

            var dto = new OrganizationResponseDto
            {
                Id = org.Id,
                OrganizationCode = org.OrganizationCode,
                OrganizationName = org.OrganizationName,
                TaxCode = org.TaxCode,
                Address = org.Address,
                Status = org.Status,
                EffectiveDate = org.EffectiveDate,
                CreatedAt = org.CreatedAt
            };

            return ApiResponse<OrganizationResponseDto>.Ok(dto);
        }

        public async Task<ApiResponse<OrganizationResponseDto>> CreateAsync(CreateOrganizationDto dto)
        {
            
            if (dto.EffectiveDate.Date < DateTime.UtcNow.Date)
            {
                return ApiResponse<OrganizationResponseDto>.Fail("Ngày hiệu lực của tổ chức phải lớn hơn hoặc bằng ngày hiện tại!");
            }

            // Kiểm tra trùng mã tổ chức
            var exists = await _context.Organizations.AnyAsync(o => o.OrganizationCode == dto.OrganizationCode && !o.IsDeleted);
            if (exists)
                return ApiResponse<OrganizationResponseDto>.Fail("Mã tổ chức này đã tồn tại trong hệ thống");

            var org = new Organization
            {
                OrganizationCode = dto.OrganizationCode.Trim(),
                OrganizationName = dto.OrganizationName.Trim(),
                TaxCode = dto.TaxCode.Trim(),
                Address = dto.Address.Trim(),
                EffectiveDate = dto.EffectiveDate,
                Status = "Còn hạn"
            };

            _context.Organizations.Add(org);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(org.Id);
        }

        public async Task<ApiResponse<OrganizationResponseDto>> UpdateAsync(Guid id, UpdateOrganizationDto dto)
        {
            var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
            if (org == null)
                return ApiResponse<OrganizationResponseDto>.Fail("Không tìm thấy tổ chức");

            org.OrganizationName = dto.OrganizationName.Trim();
            org.TaxCode = dto.TaxCode.Trim();
            org.Address = dto.Address.Trim();
            org.Status = dto.Status;
            org.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return await GetByIdAsync(org.Id);
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
            if (org == null)
                return ApiResponse<bool>.Fail("Không tìm thấy tổ chức");

            // Chỉ xóa mềm khỏi danh sách khi ở trạng thái "Hủy bỏ" (để bảo toàn lịch sử thanh toán)
            if (org.Status != "Hủy bỏ")
            {
                return ApiResponse<bool>.Fail("Tổ chức phải được chuyển sang trạng thái 'Hủy bỏ' trước khi xóa khỏi danh sách!");
            }

            // Ràng buộc: Chặn xóa nếu tổ chức đang có Cơ sở trực thuộc
            var hasFacilities = await _context.Facilities.AnyAsync(f => f.OrganizationId == id && !f.IsDeleted);
            if (hasFacilities)
                return ApiResponse<bool>.Fail("Không thể xóa tổ chức này vì đang có các cơ sở trực thuộc!");

            // Xóa mềm (Soft Delete)
            org.IsDeleted = true;
            org.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Đã xóa tổ chức thành công");
        }
    }
}
