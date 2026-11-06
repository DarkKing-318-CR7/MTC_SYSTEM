using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations;

public sealed class SubscriptionChangeConfiguration : IEntityTypeConfiguration<SubscriptionChange>
{
    public void Configure(EntityTypeBuilder<SubscriptionChange> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.CustomPackageName).HasMaxLength(255);
        b.Property(x => x.CustomPrice).HasPrecision(18, 2);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Discount).HasPrecision(18, 2);
        b.Property(x => x.AmountDue).HasPrecision(18, 2);
        b.Property(x => x.Status).IsRequired().HasMaxLength(30);
        b.Property(x => x.ChangeType).IsRequired().HasMaxLength(80);
        b.HasIndex(x => x.SubscriptionId).IsUnique().HasFilter("\"Status\" = 'Đã đặt' AND \"IsDeleted\" = false");
        b.HasOne<Subscription>().WithMany().HasForeignKey(x => x.SubscriptionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Package>().WithMany().HasForeignKey(x => x.NewPackageId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        b.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
    }
}
