using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 4] Cấu hình bảng DeviceErrorCodes + seed 8 mã lỗi kỹ thuật chuẩn theo SRS.</summary>
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

            // Dùng mốc thời gian cố định để model snapshot không thay đổi ở mỗi lần tạo migration.
            var seedCreatedAt = new DateTimeOffset(2026, 10, 22, 3, 32, 18, TimeSpan.Zero);

            // SEED: 8 mã lỗi kỹ thuật chuẩn theo SRS.
            builder.HasData(
                new DeviceErrorCode { Id = Guid.Parse("eeeeeeee-0001-0000-0000-000000000000"), ErrorCode = "MTT-CLOCK-001", ErrorName = "Sai giờ", Description = "Thời gian thiết bị sai lệch nhiều so với hệ thống", Severity = "Trung bình", CreatedAt = seedCreatedAt },
                new DeviceErrorCode { Id = Guid.Parse("eeeeeeee-0002-0000-0000-000000000000"), ErrorCode = "MTT-DOWNLOAD-002", ErrorName = "Gián đoạn tải", Description = "Thiết bị mất kết nối Internet trong khi đang tải tệp", Severity = "Trung bình", CreatedAt = seedCreatedAt },
                new DeviceErrorCode { Id = Guid.Parse("eeeeeeee-0003-0000-0000-000000000000"), ErrorCode = "MTT-DOWNLOAD-003", ErrorName = "Tải thất bại quá 3 lần", Description = "Tải tệp thất bại và thử lại quá 3 lần liên tiếp vẫn không thành công", Severity = "Nghiêm trọng", CreatedAt = seedCreatedAt },
                new DeviceErrorCode { Id = Guid.Parse("eeeeeeee-0004-0000-0000-000000000000"), ErrorCode = "MTT-RENDER-004", ErrorName = "Lỗi hiển thị độ phân giải", Description = "Độ phân giải của nội dung phát lớn hơn độ phân giải thiết bị hỗ trợ", Severity = "Trung bình", CreatedAt = seedCreatedAt },
                new DeviceErrorCode { Id = Guid.Parse("eeeeeeee-0005-0000-0000-000000000000"), ErrorCode = "MTT-FILE-005", ErrorName = "Sai checksum", Description = "Tải tệp thành công nhưng checksum không khớp", Severity = "Trung bình", CreatedAt = seedCreatedAt },
                new DeviceErrorCode { Id = Guid.Parse("eeeeeeee-0006-0000-0000-000000000000"), ErrorCode = "MTT-STORAGE-006", ErrorName = "Đầy bộ nhớ", Description = "Thiết bị không đủ không gian lưu trữ để tải thêm nội dung", Severity = "Nghiêm trọng", CreatedAt = seedCreatedAt },
                new DeviceErrorCode { Id = Guid.Parse("eeeeeeee-0007-0000-0000-000000000000"), ErrorCode = "MTT-ACC-007", ErrorName = "Tổ chức hết hạn gói", Description = "Gói cước của tổ chức đã hết hạn", Severity = "Nghiêm trọng", CreatedAt = seedCreatedAt },
                new DeviceErrorCode { Id = Guid.Parse("eeeeeeee-0008-0000-0000-000000000000"), ErrorCode = "MTT-NETWORK-008", ErrorName = "Mất kết nối Internet", Description = "Thiết bị mất kết nối Internet", Severity = "Nghiêm trọng", CreatedAt = seedCreatedAt }
            );
        }
    }
}
