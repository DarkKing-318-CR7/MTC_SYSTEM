using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 4] Cấu hình bảng DeviceErrorCodes + Seed 8 mã lỗi kỹ thuật chuẩn (MTT-001 đến MTT-008)</summary>
    public class DeviceErrorCodeConfiguration : IEntityTypeConfiguration<DeviceErrorCode>
    {
        public void Configure(EntityTypeBuilder<DeviceErrorCode> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.ErrorCode).IsRequired().HasMaxLength(50);
            builder.HasIndex(e => e.ErrorCode).IsUnique();

            builder.Property(e => e.ErrorName).IsRequired().HasMaxLength(255);
            builder.Property(e => e.Description).HasMaxLength(1000);
            builder.Property(e => e.Severity).IsRequired().HasMaxLength(50).HasDefaultValue("Trung bình");

            // SEED: 8 mã lỗi kỹ thuật chuẩn theo SRS
            builder.HasData(
                new DeviceErrorCode { Id = Guid.Parse("eeeeeeee-0001-0000-0000-000000000000"), ErrorCode = "MTT-001", ErrorName = "Mất kết nối mạng Internet", Description = "Thiết bị không thể ping đến máy chủ trung tâm trong 3 phút", Severity = "Nghiêm trọng", CreatedAt = DateTimeOffset.UtcNow },
                new DeviceErrorCode { Id = Guid.Parse("eeeeeeee-0002-0000-0000-000000000000"), ErrorCode = "MTT-002", ErrorName = "Mất nguồn điện / Pin yếu", Description = "Nguồn cấp điện không ổn định hoặc sụt áp", Severity = "Nghiêm trọng", CreatedAt = DateTimeOffset.UtcNow },
                new DeviceErrorCode { Id = Guid.Parse("eeeeeeee-0003-0000-0000-000000000000"), ErrorCode = "MTT-003", ErrorName = "Lỗi phần cứng âm thanh / Loa rè", Description = "Không nhận diện được card âm thanh hoặc tín hiệu DAC méo", Severity = "Trung bình", CreatedAt = DateTimeOffset.UtcNow },
                new DeviceErrorCode { Id = Guid.Parse("eeeeeeee-0004-0000-0000-000000000000"), ErrorCode = "MTT-004", ErrorName = "Màn hình đen / Không nhận tín hiệu HDMI", Description = "Màn hình LED/LCD không phản hồi hiển thị", Severity = "Nghiêm trọng", CreatedAt = DateTimeOffset.UtcNow },
                new DeviceErrorCode { Id = Guid.Parse("eeeeeeee-0005-0000-0000-000000000000"), ErrorCode = "MTT-005", ErrorName = "Lỗi tải tệp Media / Sai Checksum", Description = "Tệp tin tải về bị hỏng hoặc mã MD5 không khớp", Severity = "Trung bình", CreatedAt = DateTimeOffset.UtcNow },
                new DeviceErrorCode { Id = Guid.Parse("eeeeeeee-0006-0000-0000-000000000000"), ErrorCode = "MTT-006", ErrorName = "Bộ nhớ trong đầy (>95%)", Description = "Dung lượng đĩa cục bộ không đủ để lưu cache bản tin", Severity = "Trung bình", CreatedAt = DateTimeOffset.UtcNow },
                new DeviceErrorCode { Id = Guid.Parse("eeeeeeee-0007-0000-0000-000000000000"), ErrorCode = "MTT-007", ErrorName = "Nhiệt độ CPU / Thiết bị quá cao", Description = "Cảm biến nhiệt báo vượt ngưỡng an toàn (>75 độ C)", Severity = "Thấp", CreatedAt = DateTimeOffset.UtcNow },
                new DeviceErrorCode { Id = Guid.Parse("eeeeeeee-0008-0000-0000-000000000000"), ErrorCode = "MTT-008", ErrorName = "Lỗi xác thực Token / Phiên làm việc hết hạn", Description = "JWT Token của thiết bị không hợp lệ hoặc đã bị thu hồi", Severity = "Trung bình", CreatedAt = DateTimeOffset.UtcNow }
            );
        }
    }
}
