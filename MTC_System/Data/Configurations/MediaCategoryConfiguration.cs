using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 2] Cấu hình bảng MediaCategories + Seed 11 lĩnh vực mặc định</summary>
    public class MediaCategoryConfiguration : IEntityTypeConfiguration<MediaCategory>
    {
        public void Configure(EntityTypeBuilder<MediaCategory> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.CategoryName).IsRequired().HasMaxLength(255);
            builder.Property(c => c.Description).HasMaxLength(500);
            builder.Property(c => c.OrganizationId).IsRequired(false);

            // Tên danh mục là duy nhất trong cùng tổ chức hoặc trong danh mục mặc định toàn hệ thống.
            builder.HasIndex(c => new { c.OrganizationId, c.CategoryName })
                   .IsUnique()
                   .HasFilter("\"OrganizationId\" IS NOT NULL");
            builder.HasIndex(c => c.CategoryName)
                   .IsUnique()
                   .HasFilter("\"OrganizationId\" IS NULL");

            // SEED: 11 lĩnh vực mặc định toàn hệ thống
            builder.HasData(
                new MediaCategory { Id = Guid.Parse("cccccccc-0001-0000-0000-000000000000"), CategoryName = "Thông tin và Tin tức", IsDefault = true, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) },
                new MediaCategory { Id = Guid.Parse("cccccccc-0002-0000-0000-000000000000"), CategoryName = "Quảng cáo và Tiếp thị", IsDefault = true, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) },
                new MediaCategory { Id = Guid.Parse("cccccccc-0003-0000-0000-000000000000"), CategoryName = "Giáo dục", IsDefault = true, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) },
                new MediaCategory { Id = Guid.Parse("cccccccc-0004-0000-0000-000000000000"), CategoryName = "Giải trí", IsDefault = true, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) },
                new MediaCategory { Id = Guid.Parse("cccccccc-0005-0000-0000-000000000000"), CategoryName = "Sự kiện và Hội nghị", IsDefault = true, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) },
                new MediaCategory { Id = Guid.Parse("cccccccc-0006-0000-0000-000000000000"), CategoryName = "Nghệ thuật và Văn hóa", IsDefault = true, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) },
                new MediaCategory { Id = Guid.Parse("cccccccc-0007-0000-0000-000000000000"), CategoryName = "Du lịch", IsDefault = true, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) },
                new MediaCategory { Id = Guid.Parse("cccccccc-0008-0000-0000-000000000000"), CategoryName = "Sự kiện và Khuyến mãi", IsDefault = true, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) },
                new MediaCategory { Id = Guid.Parse("cccccccc-0009-0000-0000-000000000000"), CategoryName = "Giao thông và Đô thị", IsDefault = true, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) },
                new MediaCategory { Id = Guid.Parse("cccccccc-0010-0000-0000-000000000000"), CategoryName = "Y tế", IsDefault = true, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) },
                new MediaCategory { Id = Guid.Parse("cccccccc-0011-0000-0000-000000000000"), CategoryName = "Khác", IsDefault = true, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) }
            );
        }
    }
}
