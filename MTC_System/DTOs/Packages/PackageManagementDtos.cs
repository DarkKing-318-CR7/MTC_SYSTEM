using System.ComponentModel.DataAnnotations;

namespace MTC_System.DTOs.Packages;

public sealed class UpgradePackageUpsertDto
{
    [Required] public Guid PackageId { get; set; }
    [Required, StringLength(255)] public string UpgradeName { get; set; } = string.Empty;
    [StringLength(1000)] public string? Description { get; set; }
    [Range(0, int.MaxValue)] public int AdditionalDevices { get; set; }
    [Range(0, int.MaxValue)] public int AdditionalStorageGB { get; set; }
    [Range(1, 12, ErrorMessage = "Thời hạn phải là 1, 6 hoặc 12 tháng.")] public int DurationMonths { get; set; } = 1;
    public DateTimeOffset SalesStartsAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset SalesEndsAt { get; set; } = DateTimeOffset.MaxValue;
    [Range(0, double.MaxValue)] public decimal Price { get; set; }
    [StringLength(1000)] public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpgradePackageResponseDto
{
    public Guid Id { get; set; }
    public Guid PackageId { get; set; }
    public string UpgradeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int AdditionalDevices { get; set; }
    public int AdditionalStorageGB { get; set; }
    public int DurationMonths { get; set; }
    public DateTimeOffset SalesStartsAt { get; set; }
    public DateTimeOffset SalesEndsAt { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public string SalesStatus { get; set; } = string.Empty;
    public int PurchaseCount { get; set; }
}

public sealed class UpgradePurchaserDto
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTimeOffset PurchasedAt { get; set; }
}

public sealed class PackageChangeRequestDto
{
    public Guid? NewPackageId { get; set; }
    public string? CustomPackageName { get; set; }
    public int? CustomStorageGB { get; set; }
    public int? CustomMaxDevices { get; set; }
    public int? CustomDurationMonths { get; set; } // 0 = không thời hạn
    public decimal? CustomPrice { get; set; }
    public DateTimeOffset EffectiveAt { get; set; }
}

public sealed class PackageChangeQuoteDto
{
    public Guid ChangeId { get; set; }
    public decimal Amount { get; set; }
    public decimal Discount { get; set; }
    public decimal AmountDue { get; set; }
    public int BonusDays { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public DateTimeOffset EffectiveAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class SubscriptionPackageChangeResponseDto
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public Guid? NewPackageId { get; set; }
    public string? CustomPackageName { get; set; }
    public DateTimeOffset EffectiveAt { get; set; }
    public decimal Amount { get; set; }
    public decimal Discount { get; set; }
    public decimal AmountDue { get; set; }
    public int BonusDays { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
