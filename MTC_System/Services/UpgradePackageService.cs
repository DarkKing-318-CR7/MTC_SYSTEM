using Microsoft.EntityFrameworkCore;
using MTC_System.Common;
using MTC_System.Data;
using MTC_System.DTOs.Packages;
using MTC_System.Models;

namespace MTC_System.Services;

public sealed class UpgradePackageService(MyDbContext db)
{
    public async Task<ApiResponse<List<UpgradePackageResponseDto>>> GetAllAsync()
    {
        var rows = await db.UpgradePackages.AsNoTracking().Where(x => !x.IsDeleted).OrderBy(x => x.UpgradeName).ToListAsync();
        var ids = rows.Select(x => x.Id).ToList();
        var counts = await db.UpgradePurchases.Where(x => ids.Contains(x.UpgradePackageId) && !x.IsDeleted)
            .GroupBy(x => x.UpgradePackageId).Select(g => new { Id = g.Key, Count = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.Id, x => x.Count);
        return ApiResponse<List<UpgradePackageResponseDto>>.Ok(rows.Select(x => ToDto(x, counts.GetValueOrDefault(x.Id))).ToList());
    }

    public async Task<ApiResponse<UpgradePackageResponseDto>> GetByIdAsync(Guid id)
    {
        var row = await db.UpgradePackages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (row is null) return ApiResponse<UpgradePackageResponseDto>.Fail("Không tìm thấy gói nâng cấp");
        var count = await db.UpgradePurchases.Where(x => x.UpgradePackageId == id && !x.IsDeleted).SumAsync(x => (int?)x.Quantity) ?? 0;
        return ApiResponse<UpgradePackageResponseDto>.Ok(ToDto(row, count));
    }

    public async Task<ApiResponse<UpgradePackageResponseDto>> CreateAsync(UpgradePackageUpsertDto dto)
    {
        var error = await ValidateAsync(dto);
        if (error is not null) return ApiResponse<UpgradePackageResponseDto>.Fail(error);
        var row = new UpgradePackage(); Apply(row, dto); db.UpgradePackages.Add(row);
        await db.SaveChangesAsync(); return await GetByIdAsync(row.Id);
    }

    public async Task<ApiResponse<UpgradePackageResponseDto>> UpdateAsync(Guid id, UpgradePackageUpsertDto dto)
    {
        var row = await db.UpgradePackages.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (row is null) return ApiResponse<UpgradePackageResponseDto>.Fail("Không tìm thấy gói nâng cấp");
        var error = await ValidateAsync(dto);
        if (error is not null) return ApiResponse<UpgradePackageResponseDto>.Fail(error);
        Apply(row, dto); row.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(); return await GetByIdAsync(row.Id);
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
    {
        var row = await db.UpgradePackages.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (row is null) return ApiResponse<bool>.Fail("Không tìm thấy gói nâng cấp");
        if (await db.UpgradePurchases.AnyAsync(x => x.UpgradePackageId == id))
            return ApiResponse<bool>.Fail("Gói nâng cấp đã có tổ chức mua nên không được xóa");
        row.IsDeleted = true; row.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(); return ApiResponse<bool>.Ok(true, "Đã xóa gói nâng cấp");
    }

    public async Task<ApiResponse<List<UpgradePurchaserDto>>> GetPurchasersAsync(Guid id)
    {
        if (!await db.UpgradePackages.AnyAsync(x => x.Id == id && !x.IsDeleted))
            return ApiResponse<List<UpgradePurchaserDto>>.Fail("Không tìm thấy gói nâng cấp");
        var purchasers = await (from purchase in db.UpgradePurchases.AsNoTracking()
                                 join org in db.Organizations.AsNoTracking() on purchase.OrganizationId equals org.Id
                                 where purchase.UpgradePackageId == id && !purchase.IsDeleted
                                 orderby purchase.PurchasedAt descending
                                 select new UpgradePurchaserDto { OrganizationId = org.Id, OrganizationName = org.OrganizationName,
                                     Quantity = purchase.Quantity, PurchasedAt = purchase.PurchasedAt }).ToListAsync();
        return ApiResponse<List<UpgradePurchaserDto>>.Ok(purchasers);
    }

    // Invoke this only after the existing payment flow reports success.
    public async Task RecordSuccessfulPurchaseAsync(Guid upgradePackageId, Guid organizationId, Guid? subscriptionId = null)
    {
        var exists = await db.UpgradePackages.AnyAsync(x => x.Id == upgradePackageId && !x.IsDeleted && x.IsActive);
        if (!exists) throw new InvalidOperationException("Gói nâng cấp không tồn tại hoặc đã ngưng bán");
        db.UpgradePurchases.Add(new UpgradePurchase { UpgradePackageId = upgradePackageId,
            OrganizationId = organizationId, SubscriptionId = subscriptionId, Quantity = 1, PurchasedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
    }

    private async Task<string?> ValidateAsync(UpgradePackageUpsertDto d)
    {
        if (string.IsNullOrWhiteSpace(d.UpgradeName)) return "Tên gói nâng cấp là bắt buộc";
        if (d.DurationMonths is not (1 or 6 or 12)) return "Thời hạn gói nâng cấp phải là 1, 6 hoặc 12 tháng";
        if (d.Price < 0 || d.AdditionalDevices < 0 || d.AdditionalStorageGB < 0) return "Giá và tài nguyên không được âm";
        if (d.SalesEndsAt < d.SalesStartsAt) return "Ngày kết thúc mở bán phải sau ngày bắt đầu";
        if (!await db.Packages.AnyAsync(x => x.Id == d.PackageId && !x.IsDeleted && !x.IsCustom)) return "Gói cơ sở không tồn tại";
        return null;
    }

    private static void Apply(UpgradePackage p, UpgradePackageUpsertDto d)
    {
        p.PackageId = d.PackageId; p.UpgradeName = d.UpgradeName.Trim(); p.Description = d.Description;
        p.AdditionalDevices = d.AdditionalDevices; p.AdditionalStorageGB = d.AdditionalStorageGB;
        p.DurationMonths = d.DurationMonths; p.SalesStartsAt = d.SalesStartsAt; p.SalesEndsAt = d.SalesEndsAt;
        p.Price = d.Price; p.ImageUrl = d.ImageUrl; p.IsActive = d.IsActive;
    }

    private static UpgradePackageResponseDto ToDto(UpgradePackage p, int count)
    {
        var now = DateTimeOffset.UtcNow;
        var status = !p.IsActive ? "Ngưng mở bán" : now < p.SalesStartsAt ? "Chưa mở bán" : now > p.SalesEndsAt ? "Đã kết thúc" : "Đang mở bán";
        return new UpgradePackageResponseDto { Id = p.Id, PackageId = p.PackageId, UpgradeName = p.UpgradeName,
            Description = p.Description, AdditionalDevices = p.AdditionalDevices, AdditionalStorageGB = p.AdditionalStorageGB,
            DurationMonths = p.DurationMonths, SalesStartsAt = p.SalesStartsAt, SalesEndsAt = p.SalesEndsAt,
            Price = p.Price, ImageUrl = p.ImageUrl, IsActive = p.IsActive, SalesStatus = status, PurchaseCount = count };
    }
}
