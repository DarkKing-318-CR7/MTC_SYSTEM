using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 4] Cấu hình bảng DevicePlayHistories</summary>
    public class DevicePlayHistoryConfiguration : IEntityTypeConfiguration<DevicePlayHistory>
    {
        public void Configure(EntityTypeBuilder<DevicePlayHistory> builder)
        {
            builder.HasKey(h => h.Id);

            builder.Property(h => h.PlayResult).IsRequired().HasMaxLength(50).HasDefaultValue("Hoàn thành");
            builder.Property(h => h.Notes).HasMaxLength(1000);

            // Khóa ngoại sang Devices
            builder.HasOne<Device>()
                   .WithMany()
                   .HasForeignKey(h => h.DeviceId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Khóa ngoại sang Broadcasts
            builder.HasOne<Broadcast>()
                   .WithMany()
                   .HasForeignKey(h => h.BroadcastId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
