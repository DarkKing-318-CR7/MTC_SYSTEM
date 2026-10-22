using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 3] Cấu hình bảng Broadcasts</summary>
    public class BroadcastConfiguration : IEntityTypeConfiguration<Broadcast>
    {
        public void Configure(EntityTypeBuilder<Broadcast> builder)
        {
            builder.HasKey(b => b.Id);

            builder.Property(b => b.BroadcastCode).IsRequired().HasMaxLength(50);
            builder.HasIndex(b => b.BroadcastCode).IsUnique();

            builder.Property(b => b.BroadcastName).IsRequired().HasMaxLength(255);
            builder.Property(b => b.Description).HasMaxLength(2000);
            builder.Property(b => b.BroadcastType).IsRequired().HasMaxLength(50).HasDefaultValue("Thường");
            builder.Property(b => b.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Chờ duyệt");
            builder.Property(b => b.ContentType).HasMaxLength(50);
            builder.Property(b => b.RejectionReason).HasMaxLength(1000);

            builder.HasOne<Facility>()
                   .WithMany()
                   .HasForeignKey(b => b.FacilityId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Organization>()
                   .WithMany()
                   .HasForeignKey(b => b.OrganizationId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Media>()
                   .WithMany()
                   .HasForeignKey(b => b.MediaId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);

            builder.HasOne<Playlist>()
                   .WithMany()
                   .HasForeignKey(b => b.PlaylistId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);
        }
    }
}
