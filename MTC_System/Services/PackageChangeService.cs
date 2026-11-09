using Microsoft.EntityFrameworkCore;
using MTC_System.Common;
using MTC_System.Data;
using MTC_System.DTOs.Packages;
using MTC_System.Models;

namespace MTC_System.Services;

public sealed class PackageChangeService(MyDbContext db)
{
    public async Task<ApiResponse<SubscriptionPackageChangeResponseDto>> ScheduleAsync(Guid subscriptionId, PackageChangeRequestDto dto)
    {
        var sub = await db.Subscriptions.FirstOrDefaultAsync(x => x.Id == subscriptionId && !x.IsDeleted);
        if (sub is null) return ApiResponse<SubscriptionPackageChangeResponseDto>.Fail("Không tìm thấy thuê bao");
        if (await db.SubscriptionChanges.AnyAsync(x => x.SubscriptionId == subscriptionId && x.Status == "Đã đặt" && !x.IsDeleted))
            return ApiResponse<SubscriptionPackageChangeResponseDto>.Fail("Thuê bao đã có lịch đổi gói; hãy hủy lịch cũ trước");
        if (dto.NewPackageId.HasValue == !string.IsNullOrWhiteSpace(dto.CustomPackageName))
            return ApiResponse<SubscriptionPackageChangeResponseDto>.Fail("Chọn đúng một: gói sẵn hoặc gói khác");
        if (dto.NewPackageId.HasValue && sub.PackageId == dto.NewPackageId)
            return ApiResponse<SubscriptionPackageChangeResponseDto>.Fail("Không được chọn trùng gói cũ");

        var vietnamOffset = TimeSpan.FromHours(7);
        var today = DateTimeOffset.UtcNow.ToOffset(vietnamOffset).Date;
        var effectiveDate = dto.EffectiveAt.ToOffset(vietnamOffset).Date;
        if (effectiveDate <= today) return ApiResponse<SubscriptionPackageChangeResponseDto>.Fail("Ngày hiệu lực phải sau ngày hiện tại");
        if (effectiveDate > sub.EndDate.ToOffset(vietnamOffset).Date.AddDays(1))
            return ApiResponse<SubscriptionPackageChangeResponseDto>.Fail("Ngày hiệu lực không được sau ngày hết hạn gói cũ cộng 1 ngày");

        Package? next = null;
        int nextMonths;
        decimal newTotal;
        if (dto.NewPackageId.HasValue)
        {
            next = await db.Packages.FirstOrDefaultAsync(x => x.Id == dto.NewPackageId && !x.IsDeleted && !x.IsCustom);
            if (next is null) return ApiResponse<SubscriptionPackageChangeResponseDto>.Fail("Không tìm thấy gói bán sẵn");
            if (!next.IsActive || dto.EffectiveAt < next.RegistrationStartsAt || dto.EffectiveAt > next.RegistrationEndsAt)
                return ApiResponse<SubscriptionPackageChangeResponseDto>.Fail("Gói mới không mở đăng ký tại ngày hiệu lực");
            nextMonths = next.DurationMonths;
            newTotal = next.PricePerMonth * next.DurationMonths;
        }
        else
        {
            if (!dto.CustomStorageGB.HasValue || dto.CustomStorageGB <= 0 || !dto.CustomMaxDevices.HasValue || dto.CustomMaxDevices < 0 ||
                !dto.CustomDurationMonths.HasValue || dto.CustomDurationMonths < 0 || !dto.CustomPrice.HasValue || dto.CustomPrice < 0)
                return ApiResponse<SubscriptionPackageChangeResponseDto>.Fail("Gói khác cần tên, dung lượng, thiết bị, thời hạn và giá hợp lệ");
            if (await db.Packages.AnyAsync(x => !x.IsDeleted && !x.IsCustom && x.MaxStorageGB == dto.CustomStorageGB && x.DurationMonths == dto.CustomDurationMonths))
                return ApiResponse<SubscriptionPackageChangeResponseDto>.Fail("Dung lượng và thời hạn này đã có gói sẵn. Hãy chọn gói có sẵn");
            if (sub.PackageId is null && sub.CustomStorageGB == dto.CustomStorageGB && sub.CustomDurationMonths == dto.CustomDurationMonths)
                return ApiResponse<SubscriptionPackageChangeResponseDto>.Fail("Không được chọn trùng gói cũ");
            nextMonths = dto.CustomDurationMonths.Value;
            newTotal = dto.CustomPrice.Value;
        }

        var oldPackage = sub.PackageId.HasValue ? await db.Packages.FirstOrDefaultAsync(x => x.Id == sub.PackageId.Value) : null;
        var oldMonths = sub.PackageId.HasValue ? oldPackage?.DurationMonths ?? 0 : sub.CustomDurationMonths ?? 0;
        var oldDays = oldMonths == 0 ? 0 : Math.Max(1, (sub.EndDate.ToOffset(vietnamOffset).Date - sub.StartDate.ToOffset(vietnamOffset).Date).Days);
        var newDays = nextMonths == 0 ? 0 : Math.Max(1, (dto.EffectiveAt.AddMonths(nextMonths).ToOffset(vietnamOffset).Date - dto.EffectiveAt.ToOffset(vietnamOffset).Date).Days);
        var oldTotal = sub.PackageId.HasValue ? (oldPackage?.PricePerMonth ?? 0m) * (oldPackage?.DurationMonths ?? 0) : sub.CustomPrice ?? 0m;
        var quote = CalculateQuote(oldTotal, oldDays, sub.StartDate, dto.EffectiveAt, newTotal, newDays);
        var change = new SubscriptionChange
        {
            OrganizationId = sub.OrganizationId, SubscriptionId = sub.Id, NewPackageId = next?.Id,
            CustomPackageName = next is null ? dto.CustomPackageName!.Trim() : null,
            CustomStorageGB = next is null ? dto.CustomStorageGB : null,
            CustomMaxDevices = next is null ? dto.CustomMaxDevices : null,
            CustomDurationMonths = next is null ? dto.CustomDurationMonths : null,
            CustomPrice = next is null ? dto.CustomPrice : null,
            EffectiveAt = dto.EffectiveAt, Amount = quote.Amount, Discount = quote.Discount,
            AmountDue = quote.AmountDue, BonusDays = quote.BonusDays, ChangeType = quote.ChangeType
        };
        db.SubscriptionChanges.Add(change);
        await db.SaveChangesAsync();
        return ApiResponse<SubscriptionPackageChangeResponseDto>.Ok(ToDto(change), "Đã đặt lịch đổi gói");
    }

    public async Task<ApiResponse<List<SubscriptionPackageChangeResponseDto>>> GetForSubscriptionAsync(Guid subscriptionId)
    {
        if (!await db.Subscriptions.AnyAsync(x => x.Id == subscriptionId && !x.IsDeleted))
            return ApiResponse<List<SubscriptionPackageChangeResponseDto>>.Fail("Không tìm thấy thuê bao");
        var changes = await db.SubscriptionChanges.AsNoTracking().Where(x => x.SubscriptionId == subscriptionId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt).ToListAsync();
        return ApiResponse<List<SubscriptionPackageChangeResponseDto>>.Ok(changes.Select(ToDto).ToList());
    }

    public async Task<ApiResponse<bool>> CancelAsync(Guid subscriptionId, Guid changeId)
    {
        var change = await db.SubscriptionChanges.FirstOrDefaultAsync(x => x.Id == changeId && x.SubscriptionId == subscriptionId && !x.IsDeleted);
        if (change is null) return ApiResponse<bool>.Fail("Không tìm thấy lịch đổi gói");
        if (change.Status != "Đã đặt") return ApiResponse<bool>.Fail("Chỉ được hủy lịch đang ở trạng thái Đã đặt");
        change.Status = "Đã hủy"; change.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Đã hủy lịch đổi gói");
    }

    private static (decimal Amount, decimal Discount, decimal AmountDue, int BonusDays, string ChangeType)
        CalculateQuote(decimal oldTotal, int oldDays, DateTimeOffset start, DateTimeOffset effectiveAt, decimal newTotal, int newDays)
    {
        var amount = RoundMoney(newTotal);
        if (oldDays <= 0 || newDays <= 0) return (amount, 0, amount, 0, "Tính phí bình thường (không áp dụng bù)");
        var offset = TimeSpan.FromHours(7);
        var usedDays = Math.Clamp((effectiveAt.ToOffset(offset).Date - start.ToOffset(offset).Date).Days, 0, oldDays);
        var credit = Math.Max(0m, oldTotal - usedDays * (oldTotal / oldDays));
        var oldDaily = oldTotal / oldDays;
        var newDaily = newTotal / newDays;
        if (newDaily < oldDaily)
        {
            var days = newDaily <= 0 ? 0 : (int)Math.Floor(credit / newDaily);
            return (amount, 0, 0, days, "Hạ gói");
        }
        if (newDaily > oldDaily)
        {
            var discount = RoundMoney(credit);
            return (amount, discount, RoundMoney(Math.Max(0m, amount - discount)), 0, "Nâng gói");
        }
        return (amount, 0, amount, 0, "Cùng đơn giá ngày");
    }

    private static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    private static SubscriptionPackageChangeResponseDto ToDto(SubscriptionChange x) => new()
    {
        Id = x.Id, SubscriptionId = x.SubscriptionId, NewPackageId = x.NewPackageId, CustomPackageName = x.CustomPackageName,
        EffectiveAt = x.EffectiveAt, Amount = x.Amount, Discount = x.Discount, AmountDue = x.AmountDue,
        BonusDays = x.BonusDays, ChangeType = x.ChangeType, Status = x.Status
    };
}
