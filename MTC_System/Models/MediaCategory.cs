using System.Text.Json.Serialization;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 2] Bảng MediaCategories (Danh mục lĩnh vực Media)
    /// OrganizationId? = null: 11 lĩnh vực mặc định toàn hệ thống (seed)
    /// OrganizationId? != null: Lĩnh vực do riêng Tổ chức tự tạo thêm (USR-SET)
    /// </summary>
    public class MediaCategory : BaseEntity
    {
        public Guid? OrganizationId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsDefault { get; set; } = false;  // true = lĩnh vực mặc định hệ thống

        [JsonIgnore]
        public virtual ICollection<Media> MediaList { get; set; } = new List<Media>();
    }
}
