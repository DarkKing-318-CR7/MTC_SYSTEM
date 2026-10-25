using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MTC_System.Common;
using MTC_System.Data;
using MTC_System.DTOs;
using MTC_System.Models;

namespace MTC_System.Services
{
    public class FacilityService : IFacilityService
    {
        private readonly MyDbContext _context;

        public FacilityService(MyDbContext context)
        {
            _context = context;
        }

        // Dựng cây cơ sở đệ quy (Tỉnh -> Huyện -> Xã)
        public async Task<ApiResponse<List<FacilityTreeNodeDto>>> GetTreeAsync(Guid organizationId)
        {
            var allFacilities = await _context.Facilities
                .Where(f => f.OrganizationId == organizationId && !f.IsDeleted)
                .OrderBy(f => f.FacilityName)
                .ToListAsync();

            var nodeLookup = allFacilities.Select(f => new FacilityTreeNodeDto
            {
                Id = f.Id,
                OrganizationId = f.OrganizationId,
                FacilityCode = f.FacilityCode,
                FacilityName = f.FacilityName,
                IsActive = f.IsActive,
                ParentId = f.ParentId
            }).ToLookup(x => x.ParentId);

            List<FacilityTreeNodeDto> BuildTree(Guid? parentId)
            {
                return nodeLookup[parentId]
                    .Select(node =>
                    {
                        node.Children = BuildTree(node.Id);
                        return node;
                    })
                    .ToList();
            }

            var rootNodes = BuildTree(null);
            return ApiResponse<List<FacilityTreeNodeDto>>.Ok(rootNodes);
        }

        public async Task<ApiResponse<List<FacilityResponseDto>>> GetByOrganizationAsync(Guid organizationId)
        {
            var list = await _context.Facilities
                .Where(f => f.OrganizationId == organizationId && !f.IsDeleted)
                .OrderBy(f => f.FacilityCode)
                .Select(f => new FacilityResponseDto
                {
                    Id = f.Id,
                    OrganizationId = f.OrganizationId,
                    FacilityCode = f.FacilityCode,
                    FacilityName = f.FacilityName,
                    IsActive = f.IsActive,
                    ParentId = f.ParentId,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();

            return ApiResponse<List<FacilityResponseDto>>.Ok(list);
        }

        public async Task<ApiResponse<FacilityResponseDto>> GetByIdAsync(Guid id)
        {
            var f = await _context.Facilities.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (f == null)
                return ApiResponse<FacilityResponseDto>.Fail("Không tìm thấy cơ sở");

            var dto = new FacilityResponseDto
            {
                Id = f.Id,
                OrganizationId = f.OrganizationId,
                FacilityCode = f.FacilityCode,
                FacilityName = f.FacilityName,
                IsActive = f.IsActive,
                ParentId = f.ParentId,
                CreatedAt = f.CreatedAt
            };

            return ApiResponse<FacilityResponseDto>.Ok(dto);
        }

        public async Task<ApiResponse<FacilityResponseDto>> CreateAsync(CreateFacilityDto dto)
        {
            var orgExists = await _context.Organizations.AnyAsync(o => o.Id == dto.OrganizationId && !o.IsDeleted);
            if (!orgExists)
                return ApiResponse<FacilityResponseDto>.Fail("Tổ chức không tồn tại");

            var codeExists = await _context.Facilities.AnyAsync(f => f.FacilityCode == dto.FacilityCode && !f.IsDeleted);
            if (codeExists)
                return ApiResponse<FacilityResponseDto>.Fail("Mã cơ sở này đã tồn tại");

            if (dto.ParentId.HasValue)
            {
                var parent = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == dto.ParentId.Value && !f.IsDeleted);
                if (parent == null)
                    return ApiResponse<FacilityResponseDto>.Fail("Cơ sở cha không tồn tại");

                if (parent.OrganizationId != dto.OrganizationId)
                    return ApiResponse<FacilityResponseDto>.Fail("Cơ sở cha phải cùng thuộc một tổ chức");

                // Nếu cơ sở cha đang "Ngưng hoạt động", cấm tạo cơ sở con hoạt động
                if (!parent.IsActive)
                    return ApiResponse<FacilityResponseDto>.Fail("Không thể tạo cơ sở con thuộc một cơ sở cha đang ngưng hoạt động!");
            }

            var facility = new Facility
            {
                OrganizationId = dto.OrganizationId,
                FacilityCode = dto.FacilityCode.Trim(),
                FacilityName = dto.FacilityName.Trim(),
                ParentId = dto.ParentId,
                IsActive = true
            };

            _context.Facilities.Add(facility);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(facility.Id);
        }

        public async Task<ApiResponse<FacilityResponseDto>> UpdateAsync(Guid id, UpdateFacilityDto dto)
        {
            var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
            if (facility == null)
                return ApiResponse<FacilityResponseDto>.Fail("Không tìm thấy cơ sở");

            // Nếu cơ sở cha đang ngưng hoạt động, cấm bật cơ sở con hoạt động trở lại
            if (dto.IsActive && facility.ParentId.HasValue)
            {
                var parent = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == facility.ParentId.Value && !f.IsDeleted);
                if (parent != null && !parent.IsActive)
                    return ApiResponse<FacilityResponseDto>.Fail("Không thể kích hoạt cơ sở này vì cơ sở cha cấp trên đang ngưng hoạt động!");
            }

            facility.FacilityName = dto.FacilityName.Trim();
            facility.IsActive = dto.IsActive;
            facility.UpdatedAt = DateTimeOffset.UtcNow;

            // RULE ĐÓNG BĂNG CƠ SỞ CON KHI CHA DỪNG (Issue #6):
            // Nếu cơ sở cha chuyển sang ngưng hoạt động (IsActive = false),
            // tự động đóng băng toàn bộ tất cả các cơ sở con, cháu bên dưới nó!
            if (!dto.IsActive)
            {
                await DeactivateDescendantsAsync(facility.Id, facility.OrganizationId);
            }

            await _context.SaveChangesAsync();
            return await GetByIdAsync(facility.Id);
        }

        // Hàm đệ quy đóng băng toàn bộ cây con bên dưới
        private async Task DeactivateDescendantsAsync(Guid parentId, Guid organizationId)
        {
            var children = await _context.Facilities
                .Where(f => f.ParentId == parentId && f.OrganizationId == organizationId && !f.IsDeleted && f.IsActive)
                .ToListAsync();

            foreach (var child in children)
            {
                child.IsActive = false;
                child.UpdatedAt = DateTimeOffset.UtcNow;
                // Đệ quy tiếp tục đóng băng con của con
                await DeactivateDescendantsAsync(child.Id, organizationId);
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
            if (facility == null)
                return ApiResponse<bool>.Fail("Không tìm thấy cơ sở");

            // RÀNG BUỘC 1: Chặn xóa nếu cơ sở này đang có cơ sở con cấp dưới
            var hasChildren = await _context.Facilities.AnyAsync(f => f.ParentId == id && !f.IsDeleted);
            if (hasChildren)
                return ApiResponse<bool>.Fail("Không thể xóa cơ sở này vì đang có các cơ sở con cấp dưới!");

            // RÀNG BUỘC 2: Chặn xóa nếu cơ sở đang có Thiết bị (Devices của Dev 4)
            var hasDevices = await _context.Devices.AnyAsync(d => d.FacilityId == id && !d.IsDeleted);
            if (hasDevices)
                return ApiResponse<bool>.Fail("Không thể xóa cơ sở vì đang quản lý các thiết bị loa/màn hình!");

            // RÀNG BUỘC 3: Chặn xóa nếu cơ sở đang có Media (Media của Dev 2)
            var hasMedia = await _context.Media.AnyAsync(m => m.FacilityId == id && !m.IsDeleted);
            if (hasMedia)
                return ApiResponse<bool>.Fail("Không thể xóa cơ sở vì đang lưu trữ các tệp media!");

            // Xóa mềm
            facility.IsDeleted = true;
            facility.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Đã xóa cơ sở thành công");
        }
    }
}
