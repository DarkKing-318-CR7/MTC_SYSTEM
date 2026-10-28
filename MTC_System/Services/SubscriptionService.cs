using Microsoft.EntityFrameworkCore;
using MTC_System.Common;
using MTC_System.Data;
using MTC_System.DTOs.Subscriptions;
using MTC_System.Models;

namespace MTC_System.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly MyDbContext _context;

        public SubscriptionService(MyDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<List<SubscriptionResponseDto>>> GetAllAsync()
        {
            var subscriptions = await _context.Subscriptions
                .AsNoTracking()
                .Where(s => !s.IsDeleted)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new SubscriptionResponseDto
                {
                    Id = s.Id,
                    OrganizationId = s.OrganizationId,
                    PackageId = s.PackageId,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    Status = s.Status,
                    CustomStorageGB = s.CustomStorageGB,
                    CustomMaxDevices = s.CustomMaxDevices,
                    CustomDurationMonths = s.CustomDurationMonths,
                    CustomPrice = s.CustomPrice,
                    CustomPackageName = s.CustomPackageName,
                    ActualPrice = s.ActualPrice,
                    Notes = s.Notes,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                })
                .ToListAsync();

            return ApiResponse<List<SubscriptionResponseDto>>.Ok(
                subscriptions, "Lấy danh sách đăng ký thành công");
        }

        public async Task<ApiResponse<SubscriptionResponseDto>> GetByIdAsync(Guid id)
        {
            var subscription = await _context.Subscriptions
                .AsNoTracking()
                .Where(s => s.Id == id && !s.IsDeleted)
                .Select(s => new SubscriptionResponseDto
                {
                    Id = s.Id,
                    OrganizationId = s.OrganizationId,
                    PackageId = s.PackageId,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    Status = s.Status,
                    CustomStorageGB = s.CustomStorageGB,
                    CustomMaxDevices = s.CustomMaxDevices,
                    CustomDurationMonths = s.CustomDurationMonths,
                    CustomPrice = s.CustomPrice,
                    CustomPackageName = s.CustomPackageName,
                    ActualPrice = s.ActualPrice,
                    Notes = s.Notes,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (subscription == null)
            {
                return ApiResponse<SubscriptionResponseDto>.Fail(
                    "Không tìm thấy đăng ký");
            }

            return ApiResponse<SubscriptionResponseDto>.Ok(
                subscription, "Lấy thông tin đăng ký thành công");
        }

        public async Task<ApiResponse<SubscriptionResponseDto>> CreateAsync(
            SubscriptionCreateDto dto)
        {
            if (dto.EndDate <= dto.StartDate)
            {
                return ApiResponse<SubscriptionResponseDto>.Fail(
                    "Ngày kết thúc phải sau ngày bắt đầu");
            }

            var organizationExists = await _context.Organizations
                .AnyAsync(o => o.Id == dto.OrganizationId && !o.IsDeleted);

            if (!organizationExists)
            {
                return ApiResponse<SubscriptionResponseDto>.Fail(
                    "Tổ chức không tồn tại");
            }

            if (dto.PackageId.HasValue)
            {
                var packageExists = await _context.Packages
                    .AnyAsync(p => p.Id == dto.PackageId.Value && !p.IsDeleted);

                if (!packageExists)
                {
                    return ApiResponse<SubscriptionResponseDto>.Fail(
                        "Gói dịch vụ không tồn tại");
                }
            }
            else if (string.IsNullOrWhiteSpace(dto.CustomPackageName))
            {
                return ApiResponse<SubscriptionResponseDto>.Fail(
                    "Vui lòng nhập tên gói khi sử dụng gói tùy chỉnh");
            }

            var subscription = new Subscription
            {
                OrganizationId = dto.OrganizationId,
                PackageId = dto.PackageId,
                StartDate = dto.StartDate.ToUniversalTime(),
                EndDate = dto.EndDate.ToUniversalTime(),
                Status = dto.Status,
                CustomStorageGB = dto.CustomStorageGB,
                CustomMaxDevices = dto.CustomMaxDevices,
                CustomDurationMonths = dto.CustomDurationMonths,
                CustomPrice = dto.CustomPrice,
                CustomPackageName = dto.CustomPackageName,
                ActualPrice = dto.ActualPrice,
                Notes = dto.Notes
            };

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(subscription.Id);
        }

        public async Task<ApiResponse<SubscriptionResponseDto>> UpdateAsync(
            Guid id,
            SubscriptionUpdateDto dto)
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (subscription == null)
            {
                return ApiResponse<SubscriptionResponseDto>.Fail(
                    "Không tìm thấy đăng ký");
            }

            if (dto.EndDate <= dto.StartDate)
            {
                return ApiResponse<SubscriptionResponseDto>.Fail(
                    "Ngày kết thúc phải sau ngày bắt đầu");
            }

            if (dto.PackageId.HasValue)
            {
                var packageExists = await _context.Packages
                    .AnyAsync(p => p.Id == dto.PackageId.Value && !p.IsDeleted);

                if (!packageExists)
                {
                    return ApiResponse<SubscriptionResponseDto>.Fail(
                        "Gói dịch vụ không tồn tại");
                }
            }
            else if (string.IsNullOrWhiteSpace(dto.CustomPackageName))
            {
                return ApiResponse<SubscriptionResponseDto>.Fail(
                    "Vui lòng nhập tên gói khi sử dụng gói tùy chỉnh");
            }

            subscription.PackageId = dto.PackageId;
            subscription.StartDate = dto.StartDate.ToUniversalTime();
            subscription.EndDate = dto.EndDate.ToUniversalTime();
            subscription.Status = dto.Status;
            subscription.CustomStorageGB = dto.CustomStorageGB;
            subscription.CustomMaxDevices = dto.CustomMaxDevices;
            subscription.CustomDurationMonths = dto.CustomDurationMonths;
            subscription.CustomPrice = dto.CustomPrice;
            subscription.CustomPackageName = dto.CustomPackageName;
            subscription.ActualPrice = dto.ActualPrice;
            subscription.Notes = dto.Notes;
            subscription.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            return await GetByIdAsync(subscription.Id);
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (subscription == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy đăng ký");
            }

            subscription.IsDeleted = true;
            subscription.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Xóa đăng ký thành công");
        }
    }
}