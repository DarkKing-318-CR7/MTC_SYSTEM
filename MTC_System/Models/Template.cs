using System.Text.Json.Serialization;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 2] Bảng Templates (Mẫu bố cục khung hình trình chiếu đa vùng)
    /// </summary>
    public class Template : FacilityScopedEntity
    {
        public string TemplateName { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>Độ phân giải màn hình: 1920, 3840...</summary>
        public int Width { get; set; } = 1920;
        public int Height { get; set; } = 1080;

        /// <summary>JSON lưu cấu hình bố cục tổng (số zone, tỉ lệ màn hình)</summary>
        public string? LayoutConfigJson { get; set; }

        public bool IsActive { get; set; } = true;

        [JsonIgnore]
        public virtual ICollection<TemplateZone> Zones { get; set; } = new List<TemplateZone>();
    }
}
