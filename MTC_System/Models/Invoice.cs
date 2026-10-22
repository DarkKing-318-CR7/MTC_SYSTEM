using System;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 5] Bảng Invoices (Hóa đơn thanh toán gắn với hợp đồng thuê bao)
    /// </summary>
    public class Invoice : OrganizationScopedEntity
    {
        public Guid SubscriptionId { get; set; }
        public string InvoiceCode { get; set; } = string.Empty;

        public decimal Amount { get; set; } = 0;              // Số tiền hóa đơn (VNĐ)
        public decimal Discount { get; set; } = 0;            // Tiền khấu trừ (đổi gói)
        public decimal AmountDue { get; set; } = 0;           // Thực thu = Amount - Discount

        /// <summary>Trạng thái: "Chờ thanh toán" | "Đã thanh toán" | "Đã hủy"</summary>
        public string Status { get; set; } = "Chờ thanh toán";

        /// <summary>Loại giao dịch: "Đăng ký mới" | "Nâng cấp" | "Hạ cấp" | "Gia hạn"</summary>
        public string TransactionType { get; set; } = "Đăng ký mới";

        public DateTimeOffset? PaidAt { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Notes { get; set; }
    }
}
