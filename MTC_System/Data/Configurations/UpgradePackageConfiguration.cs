using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 5] Cấu hình bảng UpgradePackages</summary>
    public class UpgradePackageConfiguration : IEntityTypeConfiguration<UpgradePackage>
    {
        public void Configure(EntityTypeBuilder<UpgradePackage> builder)
        {
            builder.HasKey(u => u.Id);

            builder.Property(u => u.UpgradeName).IsRequired().HasMaxLength(255);
            builder.Property(u => u.Description).HasMaxLength(1000);
            builder.Property(u => u.Price).HasPrecision(18, 2);
            builder.Property(u => u.ImageUrl).HasMaxLength(1000);
            builder.HasIndex(u => new { u.IsActive, u.SalesStartsAt, u.SalesEndsAt });
            builder.Property(u => u.ImageUrl).HasMaxLength(1000);
            builder.HasIndex(u => new { u.IsActive, u.SalesStartsAt, u.SalesEndsAt });

            // Khóa ngoại sang Packages
            builder.HasOne<Package>()
                   .WithMany()
                   .HasForeignKey(u => u.PackageId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
