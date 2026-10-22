using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 2] Cấu hình bảng Playlists</summary>
    public class PlaylistConfiguration : IEntityTypeConfiguration<Playlist>
    {
        public void Configure(EntityTypeBuilder<Playlist> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.PlaylistName).IsRequired().HasMaxLength(255);
            builder.Property(p => p.Description).HasMaxLength(1000);
            builder.Property(p => p.PlayMode).HasMaxLength(50).HasDefaultValue("Theo thứ tự");

            builder.HasOne<Facility>()
                   .WithMany()
                   .HasForeignKey(p => p.FacilityId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Organization>()
                   .WithMany()
                   .HasForeignKey(p => p.OrganizationId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
