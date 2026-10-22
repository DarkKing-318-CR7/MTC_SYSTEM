using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 5] Cấu hình bảng Subscriptions (Hợp đồng thuê bao gói cước)</summary>
    public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
    {
        public void Configure(EntityTypeBuilder<Subscription> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Đang hoạt động");
            builder.Property(s => s.CustomPackageName).HasMaxLength(255);
            builder.Property(s => s.CustomPrice).HasPrecision(18, 2);
            builder.Property(s => s.ActualPrice).HasPrecision(18, 2);
            builder.Property(s => s.Notes).HasMaxLength(1000);

            // Liên kết Tổ chức
            builder.HasOne<Organization>()
                   .WithMany()
                   .HasForeignKey(s => s.OrganizationId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Khóa ngoại sang Packages (Nullable - hỗ trợ "Gói khác" nhập tay)
            builder.Property(s => s.PackageId).IsRequired(false);

            builder.HasOne<Package>()
                   .WithMany()
                   .HasForeignKey(s => s.PackageId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);
        }
    }
}
