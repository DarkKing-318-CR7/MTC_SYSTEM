using System;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 5] Bảng Subscriptions (Hợp đồng thuê bao gói cước của Tổ chức)
    /// Kế thừa OrganizationScopedEntity (có sẵn OrganizationId)
    /// </summary>
    public class Subscription : OrganizationScopedEntity
    {
        /// <summary>
        /// PackageId = null nếu là "Gói khác" nhập tay (SRS trang 7 - SYS-ORG-09)
        /// </summary>
        public Guid? PackageId { get; set; }

        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }

        /// <summary>Trạng thái: "Đang hoạt động" | "Hết hạn" | "Đã hủy"</summary>
        public string Status { get; set; } = "Đang hoạt động";

        // ------- "GÓI KHÁC" NHẬP TAY (Khi PackageId = null - SYS-ORG-09) -------
        public int? CustomStorageGB { get; set; }
        public int? CustomMaxDevices { get; set; }
        public int? CustomDurationMonths { get; set; }
        public decimal? CustomPrice { get; set; }
        public string? CustomPackageName { get; set; }

        // ------- THÔNG TIN THANH TOÁN -------
        public decimal ActualPrice { get; set; } = 0;         // Giá thực thu sau bù trừ đổi gói
        public string? Notes { get; set; }                    // Ghi chú hợp đồng
    }
}
