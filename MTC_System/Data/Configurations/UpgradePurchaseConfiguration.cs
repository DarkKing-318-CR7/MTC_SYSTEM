using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations;

public sealed class UpgradePurchaseConfiguration : IEntityTypeConfiguration<UpgradePurchase>
{
    public void Configure(EntityTypeBuilder<UpgradePurchase> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.UpgradePackageId, x.OrganizationId });
        b.HasOne<UpgradePackage>().WithMany().HasForeignKey(x => x.UpgradePackageId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Subscription>().WithMany().HasForeignKey(x => x.SubscriptionId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
    }
}
