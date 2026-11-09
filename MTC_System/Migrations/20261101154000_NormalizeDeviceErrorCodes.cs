using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTC_System.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeDeviceErrorCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            UpdateErrorCode(migrationBuilder, "eeeeeeee-0001-0000-0000-000000000000", "MTT-CLOCK-001", "Sai giờ", "Thời gian thiết bị sai lệch nhiều so với hệ thống", "Trung bình");
            UpdateErrorCode(migrationBuilder, "eeeeeeee-0002-0000-0000-000000000000", "MTT-DOWNLOAD-002", "Gián đoạn tải", "Thiết bị mất kết nối Internet trong khi đang tải tệp", "Trung bình");
            UpdateErrorCode(migrationBuilder, "eeeeeeee-0003-0000-0000-000000000000", "MTT-DOWNLOAD-003", "Tải thất bại quá 3 lần", "Tải tệp thất bại và thử lại quá 3 lần liên tiếp vẫn không thành công", "Nghiêm trọng");
            UpdateErrorCode(migrationBuilder, "eeeeeeee-0004-0000-0000-000000000000", "MTT-RENDER-004", "Lỗi hiển thị độ phân giải", "Độ phân giải của nội dung phát lớn hơn độ phân giải thiết bị hỗ trợ", "Trung bình");
            UpdateErrorCode(migrationBuilder, "eeeeeeee-0005-0000-0000-000000000000", "MTT-FILE-005", "Sai checksum", "Tải tệp thành công nhưng checksum không khớp", "Trung bình");
            UpdateErrorCode(migrationBuilder, "eeeeeeee-0006-0000-0000-000000000000", "MTT-STORAGE-006", "Đầy bộ nhớ", "Thiết bị không đủ không gian lưu trữ để tải thêm nội dung", "Nghiêm trọng");
            UpdateErrorCode(migrationBuilder, "eeeeeeee-0007-0000-0000-000000000000", "MTT-ACC-007", "Tổ chức hết hạn gói", "Gói cước của tổ chức đã hết hạn", "Nghiêm trọng");
            UpdateErrorCode(migrationBuilder, "eeeeeeee-0008-0000-0000-000000000000", "MTT-NETWORK-008", "Mất kết nối Internet", "Thiết bị mất kết nối Internet", "Nghiêm trọng");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            UpdateErrorCode(migrationBuilder, "eeeeeeee-0001-0000-0000-000000000000", "MTT-001", "Mất kết nối mạng Internet", "Thiết bị không thể ping đến máy chủ trung tâm trong 3 phút", "Nghiêm trọng");
            UpdateErrorCode(migrationBuilder, "eeeeeeee-0002-0000-0000-000000000000", "MTT-002", "Mất nguồn điện / Pin yếu", "Nguồn cấp điện không ổn định hoặc sụt áp", "Nghiêm trọng");
            UpdateErrorCode(migrationBuilder, "eeeeeeee-0003-0000-0000-000000000000", "MTT-003", "Lỗi phần cứng âm thanh / Loa rè", "Không nhận diện được card âm thanh hoặc tín hiệu DAC méo", "Trung bình");
            UpdateErrorCode(migrationBuilder, "eeeeeeee-0004-0000-0000-000000000000", "MTT-004", "Màn hình đen / Không nhận tín hiệu HDMI", "Màn hình LED/LCD không phản hồi hiển thị", "Nghiêm trọng");
            UpdateErrorCode(migrationBuilder, "eeeeeeee-0005-0000-0000-000000000000", "MTT-005", "Lỗi tải tệp Media / Sai Checksum", "Tệp tin tải về bị hỏng hoặc mã MD5 không khớp", "Trung bình");
            UpdateErrorCode(migrationBuilder, "eeeeeeee-0006-0000-0000-000000000000", "MTT-006", "Bộ nhớ trong đầy (>95%)", "Dung lượng đĩa cục bộ không đủ để lưu cache bản tin", "Trung bình");
            UpdateErrorCode(migrationBuilder, "eeeeeeee-0007-0000-0000-000000000000", "MTT-007", "Nhiệt độ CPU / Thiết bị quá cao", "Cảm biến nhiệt báo vượt ngưỡng an toàn (>75 độ C)", "Thấp");
            UpdateErrorCode(migrationBuilder, "eeeeeeee-0008-0000-0000-000000000000", "MTT-008", "Lỗi xác thực Token / Phiên làm việc hết hạn", "JWT Token của thiết bị không hợp lệ hoặc đã bị thu hồi", "Trung bình");
        }

        private static void UpdateErrorCode(
            MigrationBuilder migrationBuilder,
            string id,
            string errorCode,
            string errorName,
            string description,
            string severity)
        {
            migrationBuilder.UpdateData(
                table: "DeviceErrorCodes",
                keyColumn: "Id",
                keyValue: new Guid(id),
                columns: new[] { "Description", "ErrorCode", "ErrorName", "Severity" },
                values: new object[] { description, errorCode, errorName, severity });
        }
    }
}
