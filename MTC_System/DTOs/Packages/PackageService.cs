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
            var now = DateTimeOffset.UtcNow;
            var packages = await _context.Packages
                .AsNoTracking()
                .Where(p => !p.IsDeleted && !p.IsCustom)
                .OrderBy(p => p.MaxStorageGB)
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
                    RegistrationStartsAt = p.RegistrationStartsAt,
                    RegistrationEndsAt = p.RegistrationEndsAt,
                    Status = !p.IsActive ? "Ngưng kinh doanh" : now < p.RegistrationStartsAt ? "Chưa kinh doanh" : now > p.RegistrationEndsAt ? "Đã kết thúc" : "Đang kinh doanh",
                    IsActive = p.IsActive,
                    IsCustom = p.IsCustom
                })
                .ToListAsync();

            return ApiResponse<List<PackageResponseDto>>.Ok(packages);
        }

        // Lấy chi tiết gói theo ID
        public async Task<ApiResponse<PackageResponseDto>> GetByIdAsync(Guid id)
        {
            var now = DateTimeOffset.UtcNow;
            var package = await _context.Packages
                .AsNoTracking()
                .Where(p => p.Id == id && !p.IsDeleted && !p.IsCustom)
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
                    RegistrationStartsAt = p.RegistrationStartsAt,
                    RegistrationEndsAt = p.RegistrationEndsAt,
                    Status = !p.IsActive ? "Ngưng kinh doanh" : now < p.RegistrationStartsAt ? "Chưa kinh doanh" : now > p.RegistrationEndsAt ? "Đã kết thúc" : "Đang kinh doanh",
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
                dto.MaxStorageGB <= 0 || dto.IsCustom || dto.RegistrationEndsAt < dto.RegistrationStartsAt)
            {
                return ApiResponse<PackageResponseDto>.Fail(
                    "Giá, số thiết bị và dung lượng không được âm");
            }

            var catalogError = await ValidateCatalogAsync(dto.MaxStorageGB, dto.DurationMonths,
                dto.RegistrationStartsAt, dto.RegistrationEndsAt, null);
            if (catalogError != null) return ApiResponse<PackageResponseDto>.Fail(catalogError);

            var package = new Package
            {
                PackageCode = code,
                PackageName = dto.PackageName.Trim(),
                Description = dto.Description?.Trim(),
                PricePerMonth = dto.PricePerMonth,
                MaxDevices = dto.MaxDevices,
                MaxStorageGB = dto.MaxStorageGB,
                DurationMonths = dto.DurationMonths,
                RegistrationStartsAt = dto.RegistrationStartsAt,
                RegistrationEndsAt = dto.RegistrationEndsAt,
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
                dto.MaxStorageGB <= 0 || dto.IsCustom || dto.RegistrationEndsAt < dto.RegistrationStartsAt)
            {
                return ApiResponse<PackageResponseDto>.Fail(
                    "Giá, số thiết bị và dung lượng không được âm");
            }

            var catalogError = await ValidateCatalogAsync(dto.MaxStorageGB, dto.DurationMonths,
                dto.RegistrationStartsAt, dto.RegistrationEndsAt, id);
            if (catalogError != null) return ApiResponse<PackageResponseDto>.Fail(catalogError);

            var wasActive = package.IsActive;

            package.PackageCode = code;
            package.PackageName = dto.PackageName.Trim();
            package.Description = dto.Description?.Trim();
            package.PricePerMonth = dto.PricePerMonth;
            package.MaxDevices = dto.MaxDevices;
            package.MaxStorageGB = dto.MaxStorageGB;
            package.DurationMonths = dto.DurationMonths;
            package.RegistrationStartsAt = dto.RegistrationStartsAt;
            package.RegistrationEndsAt = dto.RegistrationEndsAt;
            package.IsActive = dto.IsActive;
            package.IsCustom = dto.IsCustom;
            package.UpdatedAt = DateTimeOffset.UtcNow;

            if (wasActive && !package.IsActive)
                await StopRenewalAndNotifyAsync(package);

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

            var now = DateTimeOffset.UtcNow;
            if (package.IsActive && now >= package.RegistrationStartsAt && now <= package.RegistrationEndsAt)
            {
                return ApiResponse<bool>.Fail(
                    "Chỉ được xóa gói Chưa kinh doanh, Ngưng kinh doanh hoặc Đã kết thúc");
            }

            package.IsDeleted = true;
            package.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Ok(
                true,
                "Đã xóa mềm gói dịch vụ thành công");
        }

        private async Task<string?> ValidateCatalogAsync(int storageGB, int durationMonths,
            DateTimeOffset startsAt, DateTimeOffset endsAt, Guid? excludedId)
        {
            var packages = await _context.Packages.AsNoTracking()
                .Where(p => !p.IsDeleted && !p.IsCustom && p.Id != excludedId).ToListAsync();
            if (packages.Any(p => p.MaxStorageGB == storageGB))
                return "Dung lượng GB này đã được dùng bởi một gói bán sẵn khác";
            if (packages.Any(p => p.DurationMonths == durationMonths && startsAt <= p.RegistrationEndsAt && p.RegistrationStartsAt <= endsAt))
                return "Khoảng thời gian áp dụng bị trùng với gói cùng thời hạn";
            return null;
        }

        private async Task StopRenewalAndNotifyAsync(Package package)
        {
            var subscriptions = await _context.Subscriptions.Where(s => !s.IsDeleted && s.PackageId == package.Id &&
                s.Status == "Đang hoạt động" && s.EndDate >= DateTimeOffset.UtcNow).ToListAsync();
            var organizationIds = subscriptions.Select(s => s.OrganizationId).Distinct().ToList();
            foreach (var subscription in subscriptions) subscription.AutoRenew = false;
            if (organizationIds.Count == 0) return;

            var admins = await (from user in _context.Users
                                join userRole in _context.UserRoles on user.Id equals userRole.UserId
                                join role in _context.Roles on userRole.RoleId equals role.Id
                                where user.IsActive && user.OrganizationId != null && organizationIds.Contains(user.OrganizationId.Value)
                                    && !user.IsDeleted && !userRole.IsDeleted && !role.IsDeleted
                                    && (role.RoleCode.ToUpper() == "ADMINISTRATOR" || role.RoleCode.ToUpper() == "ORG_ADMIN")
                                select new { user.Id, OrganizationId = user.OrganizationId!.Value }).ToListAsync();

            foreach (var organizationId in organizationIds)
            {
                var recipients = admins.Where(x => x.OrganizationId == organizationId).ToList();
                if (recipients.Count == 0) _context.SystemNotifications.Add(MakeNotification(organizationId, null, package));
                else foreach (var admin in recipients) _context.SystemNotifications.Add(MakeNotification(organizationId, admin.Id, package));
            }
        }

        private static SystemNotification MakeNotification(Guid organizationId, Guid? userId, Package package) => new()
        {
            OrganizationId = organizationId,
            TargetUserId = userId,
            Title = "Gói cước sử dụng đã ngưng kinh doanh",
            Content = $"Gói cước {package.MaxStorageGB}GB/{package.DurationMonths}M đang sử dụng đã ngưng kinh doanh. Chọn gói cước khác để tiếp tục sử dụng khi hết hạn.",
            NotificationType = "USR-PKG-02",
            IsSent = true,
            SentAt = DateTimeOffset.UtcNow
        };
    }
}
