using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 2] Cấu hình bảng PlaylistItems</summary>
    public class PlaylistItemConfiguration : IEntityTypeConfiguration<PlaylistItem>
    {
        public void Configure(EntityTypeBuilder<PlaylistItem> builder)
        {
            builder.HasKey(i => i.Id);

            // Mỗi Media chỉ xuất hiện 1 lần trong 1 Playlist
            builder.HasIndex(i => new { i.PlaylistId, i.MediaId }).IsUnique();

            // Khóa ngoại sang Playlists
            builder.HasOne<Playlist>()
                   .WithMany(p => p.Items)
                   .HasForeignKey(i => i.PlaylistId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Khóa ngoại sang Media
            builder.HasOne<Media>()
                   .WithMany()
                   .HasForeignKey(i => i.MediaId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
