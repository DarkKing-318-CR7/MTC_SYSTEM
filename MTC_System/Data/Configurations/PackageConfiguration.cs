using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 5] Cấu hình bảng Packages + Seed 3 gói cước mẫu</summary>
    public class PackageConfiguration : IEntityTypeConfiguration<Package>
    {
        public void Configure(EntityTypeBuilder<Package> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.PackageCode).IsRequired().HasMaxLength(50);
            builder.HasIndex(p => p.PackageCode).IsUnique();

            builder.Property(p => p.PackageName).IsRequired().HasMaxLength(255);
            builder.Property(p => p.Description).HasMaxLength(1000);
            builder.Property(p => p.PricePerMonth).HasPrecision(18, 2);

            // SEED: 3 gói cước bán sẵn mặc định
            builder.HasData(
                new Package
                {
                    Id = Guid.Parse("ffffffff-0001-0000-0000-000000000000"),
                    PackageCode = "PKG-BASIC",
                    PackageName = "Gói Cơ Bản (Khởi nghiệp)",
                    Description = "Dành cho cơ sở quy mô nhỏ, tối đa 5 màn hình/loa, dung lượng 50GB",
                    PricePerMonth = 500000m,
                    MaxDevices = 5,
                    MaxStorageGB = 50,
                    DurationMonths = 1,
                    IsActive = true,
                    IsCustom = false,
                    CreatedAt = DateTimeOffset.UtcNow
                },
                new Package
                {
                    Id = Guid.Parse("ffffffff-0002-0000-0000-000000000000"),
                    PackageCode = "PKG-STANDARD",
                    PackageName = "Gói Tiêu Chuẩn (Doanh nghiệp)",
                    Description = "Dành cho cơ sở quy mô vừa, tối đa 20 màn hình/loa, dung lượng 200GB",
                    PricePerMonth = 1500000m,
                    MaxDevices = 20,
                    MaxStorageGB = 200,
                    DurationMonths = 6,
                    IsActive = true,
                    IsCustom = false,
                    CreatedAt = DateTimeOffset.UtcNow
                },
                new Package
                {
                    Id = Guid.Parse("ffffffff-0003-0000-0000-000000000000"),
                    PackageCode = "PKG-ENTERPRISE",
                    PackageName = "Gói Cao Cấp (Tập đoàn)",
                    Description = "Dành cho chuỗi cơ sở lớn, tối đa 100 màn hình/loa, dung lượng 1000GB (1TB)",
                    PricePerMonth = 5000000m,
                    MaxDevices = 100,
                    MaxStorageGB = 1000,
                    DurationMonths = 12,
                    IsActive = true,
                    IsCustom = false,
                    CreatedAt = DateTimeOffset.UtcNow
                }
            );
        }
    }
}
