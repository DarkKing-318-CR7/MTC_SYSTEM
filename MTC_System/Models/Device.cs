using System;
using System.Text.Json.Serialization;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 4] Bảng Devices (Thiết bị màn hình / cụm loa thông minh)
    /// </summary>
    public class Device : FacilityScopedEntity
    {
        public string DeviceCode { get; set; } = string.Empty;  // Mã định danh duy nhất
        public string DeviceName { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }

        public string? IpAddress { get; set; }
        public string? SimPhoneNumber { get; set; }
        public string? SimSerialNumber { get; set; }
        public string? Tag { get; set; }                         // Nhãn phân loại tùy chỉnh

        /// <summary>HĐH: "Android" | "Linux" | "Windows" (Phục vụ báo cáo tỉ lệ HĐH)</summary>
        public string OsType { get; set; } = "Android";

        public string? VendorName { get; set; }                  // Tên nhà cung cấp (mặc định: Alta Media)

        // Khóa ngoại: Nhóm thiết bị (Nullable - thiết bị có thể chưa thuộc nhóm nào)
        public Guid? DeviceGroupId { get; set; }

        // ------- 3 TRẠNG THÁI ĐỘC LẬP (SRS trang 11 - BẮT BUỘC TÁCH RIÊNG) -------
        /// <summary>Trạng thái hoạt động do người dùng bật/tắt thủ công</summary>
        public bool ActiveStatus { get; set; } = true;

        /// <summary>Trạng thái kết nối do SignalR cập nhật: "Đang kết nối" | "Mất kết nối"</summary>
        public string ConnectionStatus { get; set; } = "Mất kết nối";

        /// <summary>Trạng thái phát do thiết bị gửi lên: "Đang phát bản tin" | "Dừng phát bản tin"</summary>
        public string PlayStatus { get; set; } = "Dừng phát bản tin";

        // ------- ÂM LƯỢNG & ẢNH CHỤP MÀN HÌNH -------
        /// <summary>Mức âm lượng hiện tại (0-100, mặc định 50 khi điều khiển hàng loạt)</summary>
        public int Volume { get; set; } = 50;

        public string? ScreenshotUrl { get; set; }               // Ảnh chụp màn hình gần nhất
        public long ScreenshotSizeInBytes { get; set; } = 0;     // Phục vụ báo cáo dung lượng SYS-RPT-04
        public DateTimeOffset? LastScreenshotAt { get; set; }

        // ------- TỌA ĐỘ GIS (Bản đồ số Dashboard) -------
        public double Latitude { get; set; } = 10.7698780;
        public double Longitude { get; set; } = 106.6511720;
        public string? InstallationAddress { get; set; }

        // ------- QR CODE / OTP ĐĂNG NHẬP THIẾT BỊ (USR-DEV-09) -------
        public string? ActivationQrToken { get; set; }          // Mã QR token (dùng 1 lần)
        public string? ActivationOtp { get; set; }              // Mã OTP 6 số (dùng 1 lần)
        public DateTimeOffset? ActivationTokenExpiry { get; set; }
        public bool IsActivated { get; set; } = false;

        public DateTimeOffset? LastOnlineAt { get; set; }       // Lần cuối kết nối

        [JsonIgnore]
        public virtual DeviceGroup? DeviceGroup { get; set; }
    }
}
