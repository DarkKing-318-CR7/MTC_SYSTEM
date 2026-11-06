using MTC_System.Common;

namespace MTC_System.Models;

public class UpgradePurchase : OrganizationScopedEntity
{
    public Guid UpgradePackageId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public int Quantity { get; set; } = 1;
    public DateTimeOffset PurchasedAt { get; set; } = DateTimeOffset.UtcNow;
}
