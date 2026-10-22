using System;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 2] Bảng TemplateZones (Vùng phát bên trong Template)
    /// Lưu tọa độ pixel X, Y, chiều rộng%, chiều cao%, Z-Index và loại media cho phép
    /// CẤM đưa nguồn FM/Hz hoặc Stream trực tiếp vào zone của Template
    /// </summary>
    public class TemplateZone : BaseEntity
    {
        public Guid TemplateId { get; set; }
        public string ZoneName { get; set; } = string.Empty;

        /// <summary>Tọa độ pixel vị trí zone trên màn hình</summary>
        public int X { get; set; } = 0;
        public int Y { get; set; } = 0;

        /// <summary>Kích thước zone theo tỉ lệ % màn hình (0.0 - 100.0)</summary>
        public decimal WidthPercent { get; set; } = 100;
        public decimal HeightPercent { get; set; } = 100;

        /// <summary>Thứ tự lớp hiển thị (Z-Index, 0 = nền)</summary>
        public int ZIndex { get; set; } = 0;

        /// <summary>Loại media được phép: Video, Audio, Image, URL (CẤM Hz_FM và Stream)</summary>
        public string AllowedMediaTypes { get; set; } = "Video,Audio,Image,URL";
    }
}
