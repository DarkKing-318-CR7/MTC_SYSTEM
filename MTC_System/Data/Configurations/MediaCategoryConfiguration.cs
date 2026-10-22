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

            // Tên danh mục là duy nhất trong cùng 1 Tổ chức (null = toàn hệ thống)
            builder.HasIndex(c => new { c.OrganizationId, c.CategoryName }).IsUnique();

            // SEED: 11 lĩnh vực mặc định toàn hệ thống
            builder.HasData(
                new MediaCategory { Id = Guid.Parse("cccccccc-0001-0000-0000-000000000000"), CategoryName = "Chính trị - Xã hội", IsDefault = true, CreatedAt = DateTimeOffset.UtcNow },
                new MediaCategory { Id = Guid.Parse("cccccccc-0002-0000-0000-000000000000"), CategoryName = "Kinh tế", IsDefault = true, CreatedAt = DateTimeOffset.UtcNow },
                new MediaCategory { Id = Guid.Parse("cccccccc-0003-0000-0000-000000000000"), CategoryName = "Văn hóa - Nghệ thuật", IsDefault = true, CreatedAt = DateTimeOffset.UtcNow },
                new MediaCategory { Id = Guid.Parse("cccccccc-0004-0000-0000-000000000000"), CategoryName = "Giáo dục", IsDefault = true, CreatedAt = DateTimeOffset.UtcNow },
                new MediaCategory { Id = Guid.Parse("cccccccc-0005-0000-0000-000000000000"), CategoryName = "Y tế - Sức khỏe", IsDefault = true, CreatedAt = DateTimeOffset.UtcNow },
                new MediaCategory { Id = Guid.Parse("cccccccc-0006-0000-0000-000000000000"), CategoryName = "Thể thao", IsDefault = true, CreatedAt = DateTimeOffset.UtcNow },
                new MediaCategory { Id = Guid.Parse("cccccccc-0007-0000-0000-000000000000"), CategoryName = "An ninh - Quốc phòng", IsDefault = true, CreatedAt = DateTimeOffset.UtcNow },
                new MediaCategory { Id = Guid.Parse("cccccccc-0008-0000-0000-000000000000"), CategoryName = "Khoa học - Công nghệ", IsDefault = true, CreatedAt = DateTimeOffset.UtcNow },
                new MediaCategory { Id = Guid.Parse("cccccccc-0009-0000-0000-000000000000"), CategoryName = "Môi trường", IsDefault = true, CreatedAt = DateTimeOffset.UtcNow },
                new MediaCategory { Id = Guid.Parse("cccccccc-0010-0000-0000-000000000000"), CategoryName = "Quảng cáo", IsDefault = true, CreatedAt = DateTimeOffset.UtcNow },
                new MediaCategory { Id = Guid.Parse("cccccccc-0011-0000-0000-000000000000"), CategoryName = "Khác", IsDefault = true, CreatedAt = DateTimeOffset.UtcNow }
            );
        }
    }
}
