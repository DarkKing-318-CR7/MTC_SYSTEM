using MTC_System.Common;

namespace MTC_System.Models;

public class SubscriptionChange : OrganizationScopedEntity
{
    public Guid SubscriptionId { get; set; }
    public Guid? NewPackageId { get; set; }
    public string? CustomPackageName { get; set; }
    public int? CustomStorageGB { get; set; }
    public int? CustomMaxDevices { get; set; }
    public int? CustomDurationMonths { get; set; } // 0 = gói không thời hạn
    public decimal? CustomPrice { get; set; }
    public DateTimeOffset EffectiveAt { get; set; }
    public decimal Amount { get; set; }
    public decimal Discount { get; set; }
    public decimal AmountDue { get; set; }
    public int BonusDays { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string Status { get; set; } = "Đã đặt";
}
