using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MTC_System.Common;
using MTC_System.Data;
using MTC_System.DTOs.Packages;
using MTC_System.Models;

namespace MTC_System.Services
{
    public class PackageService : IPackageService
    {
        private readonly MyDbContext _context;

        public PackageService(MyDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách gói dịch vụ chưa bị xóa
        public async Task<ApiResponse<List<PackageResponseDto>>> GetAllAsync()
        {
            var packages = await _context.Packages
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.PricePerMonth)
                .Select(p => new PackageResponseDto
                {
                    Id = p.Id,
                    PackageCode = p.PackageCode,
                    PackageName = p.PackageName,
                    Description = p.Description,
                    PricePerMonth = p.PricePerMonth,
                    MaxDevices = p.MaxDevices,
                    MaxStorageGB = p.MaxStorageGB,
                    DurationMonths = p.DurationMonths,
                    IsActive = p.IsActive,
                    IsCustom = p.IsCustom
                })
                .ToListAsync();

            return ApiResponse<List<PackageResponseDto>>.Ok(packages);
        }

        // Lấy chi tiết gói theo ID
        public async Task<ApiResponse<PackageResponseDto>> GetByIdAsync(Guid id)
        {
            var package = await _context.Packages
                .AsNoTracking()
                .Where(p => p.Id == id && !p.IsDeleted)
                .Select(p => new PackageResponseDto
                {
                    Id = p.Id,
                    PackageCode = p.PackageCode,
                    PackageName = p.PackageName,
                    Description = p.Description,
                    PricePerMonth = p.PricePerMonth,
                    MaxDevices = p.MaxDevices,
                    MaxStorageGB = p.MaxStorageGB,
                    DurationMonths = p.DurationMonths,
                    IsActive = p.IsActive,
                    IsCustom = p.IsCustom
                })
                .FirstOrDefaultAsync();

            if (package == null)
                return ApiResponse<PackageResponseDto>.Fail(
                    "Không tìm thấy gói dịch vụ");

            return ApiResponse<PackageResponseDto>.Ok(package);
        }

        // Tạo gói dịch vụ
        public async Task<ApiResponse<PackageResponseDto>> CreateAsync(
            PackageCreateDto dto)
        {
            var code = dto.PackageCode.Trim();

            var codeExists = await _context.Packages
                .AnyAsync(p => p.PackageCode == code);

            if (codeExists)
            {
                return ApiResponse<PackageResponseDto>.Fail(
                    "Mã gói dịch vụ đã tồn tại");
            }

            if (dto.DurationMonths != 1 &&
                dto.DurationMonths != 6 &&
                dto.DurationMonths != 12)
            {
                return ApiResponse<PackageResponseDto>.Fail(
                    "Thời hạn gói phải là 1, 6 hoặc 12 tháng");
            }

            if (dto.PricePerMonth < 0 ||
                dto.MaxDevices < 0 ||
                dto.MaxStorageGB < 0)
            {
                return ApiResponse<PackageResponseDto>.Fail(
                    "Giá, số thiết bị và dung lượng không được âm");
            }

            var package = new Package
            {
                PackageCode = code,
                PackageName = dto.PackageName.Trim(),
                Description = dto.Description?.Trim(),
                PricePerMonth = dto.PricePerMonth,
                MaxDevices = dto.MaxDevices,
                MaxStorageGB = dto.MaxStorageGB,
                DurationMonths = dto.DurationMonths,
                IsActive = dto.IsActive,
                IsCustom = dto.IsCustom
            };

            _context.Packages.Add(package);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(package.Id);
        }

        // Cập nhật gói dịch vụ
        public async Task<ApiResponse<PackageResponseDto>> UpdateAsync(
            Guid id,
            PackageUpdateDto dto)
        {
            var package = await _context.Packages
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (package == null)
            {
                return ApiResponse<PackageResponseDto>.Fail(
                    "Không tìm thấy gói dịch vụ");
            }

            var code = dto.PackageCode.Trim();

            var codeExists = await _context.Packages
                .AnyAsync(p => p.Id != id && p.PackageCode == code);

            if (codeExists)
            {
                return ApiResponse<PackageResponseDto>.Fail(
                    "Mã gói dịch vụ đã được sử dụng");
            }

            if (dto.DurationMonths != 1 &&
                dto.DurationMonths != 6 &&
                dto.DurationMonths != 12)
            {
                return ApiResponse<PackageResponseDto>.Fail(
                    "Thời hạn gói phải là 1, 6 hoặc 12 tháng");
            }

            if (dto.PricePerMonth < 0 ||
                dto.MaxDevices < 0 ||
                dto.MaxStorageGB < 0)
            {
                return ApiResponse<PackageResponseDto>.Fail(
                    "Giá, số thiết bị và dung lượng không được âm");
            }

            package.PackageCode = code;
            package.PackageName = dto.PackageName.Trim();
            package.Description = dto.Description?.Trim();
            package.PricePerMonth = dto.PricePerMonth;
            package.MaxDevices = dto.MaxDevices;
            package.MaxStorageGB = dto.MaxStorageGB;
            package.DurationMonths = dto.DurationMonths;
            package.IsActive = dto.IsActive;
            package.IsCustom = dto.IsCustom;
            package.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            return await GetByIdAsync(package.Id);
        }

        // Xóa mềm gói dịch vụ
        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            var package = await _context.Packages
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (package == null)
            {
                return ApiResponse<bool>.Fail(
                    "Không tìm thấy gói dịch vụ");
            }

            // Không xóa gói nếu đã được sử dụng trong đăng ký còn hiệu lực.
            // Cần đối chiếu quy tắc trạng thái Subscription của dự án.
            var hasSubscriptions = await _context.Subscriptions
                .AnyAsync(s => s.PackageId == id && !s.IsDeleted);

            if (hasSubscriptions)
            {
                return ApiResponse<bool>.Fail(
                    "Gói đã có đăng ký sử dụng. Hãy ngừng hoạt động " +
                    "thay vì xóa để bảo toàn lịch sử");
            }

            package.IsDeleted = true;
            package.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Ok(
                true,
                "Đã xóa mềm gói dịch vụ thành công");
        }
    }
}