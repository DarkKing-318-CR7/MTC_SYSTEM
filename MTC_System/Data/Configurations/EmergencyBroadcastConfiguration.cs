using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 3] Cấu hình bảng EmergencyBroadcasts</summary>
    public class EmergencyBroadcastConfiguration : IEntityTypeConfiguration<EmergencyBroadcast>
    {
        public void Configure(EntityTypeBuilder<EmergencyBroadcast> builder)
        {
            builder.HasKey(eb => eb.Id);

            builder.Property(eb => eb.BroadcastScope).IsRequired().HasMaxLength(50).HasDefaultValue("Tất cả thiết bị");
            builder.Property(eb => eb.TargetIdsJson).HasColumnType("text");
            builder.Property(eb => eb.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Đang phát");

            // Khóa ngoại sang Broadcasts
            builder.HasOne<Broadcast>()
                   .WithMany()
                   .HasForeignKey(eb => eb.BroadcastId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Khóa ngoại sang Facilities
            builder.HasOne<Facility>()
                   .WithMany()
                   .HasForeignKey(eb => eb.FacilityId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
