using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 3] Cấu hình bảng BroadcastDevices</summary>
    public class BroadcastDeviceConfiguration : IEntityTypeConfiguration<BroadcastDevice>
    {
        public void Configure(EntityTypeBuilder<BroadcastDevice> builder)
        {
            builder.HasKey(bd => bd.Id);

            builder.Property(bd => bd.DeviceStatus).IsRequired().HasMaxLength(50).HasDefaultValue("Chờ phát");

            // Mỗi cặp Schedule + Device là duy nhất
            builder.HasIndex(bd => new { bd.BroadcastScheduleId, bd.DeviceId }).IsUnique();

            // Khóa ngoại sang BroadcastSchedules
            builder.HasOne<BroadcastSchedule>()
                   .WithMany(s => s.BroadcastDevices)
                   .HasForeignKey(bd => bd.BroadcastScheduleId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Khóa ngoại sang Devices
            builder.HasOne<Device>()
                   .WithMany()
                   .HasForeignKey(bd => bd.DeviceId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
