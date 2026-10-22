using System.Text.Json.Serialization;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 2] Bảng Playlists (Danh sách phát gom nhiều Media)
    /// </summary>
    public class Playlist : FacilityScopedEntity
    {
        public string PlaylistName { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>Chế độ phát: "Theo thứ tự" | "Ngẫu nhiên"</summary>
        public string PlayMode { get; set; } = "Theo thứ tự";

        /// <summary>Tổng thời lượng (giây) - tự tính từ các PlaylistItem</summary>
        public int TotalDurationSeconds { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        [JsonIgnore]
        public virtual ICollection<PlaylistItem> Items { get; set; } = new List<PlaylistItem>();
    }
}
