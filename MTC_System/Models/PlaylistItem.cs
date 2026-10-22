using System;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 2] Bảng PlaylistItems (Thứ tự các Media trong Playlist)
    /// </summary>
    public class PlaylistItem : BaseEntity
    {
        public Guid PlaylistId { get; set; }
        public Guid MediaId { get; set; }

        /// <summary>Thứ tự phát (1, 2, 3...)</summary>
        public int DisplayOrder { get; set; } = 1;

        /// <summary>Thời lượng phát item này (giây) - ghi đè DurationSeconds của Media nếu cần</summary>
        public int DurationSeconds { get; set; } = 0;
    }
}
