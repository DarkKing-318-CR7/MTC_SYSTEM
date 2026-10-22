using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 4] Cấu hình bảng DeviceActivityLogs</summary>
    public class DeviceActivityLogConfiguration : IEntityTypeConfiguration<DeviceActivityLog>
    {
        public void Configure(EntityTypeBuilder<DeviceActivityLog> builder)
        {
            builder.HasKey(l => l.Id);

            builder.Property(l => l.ActionType).IsRequired().HasMaxLength(50);
            builder.Property(l => l.ActionDetail).HasColumnType("text");
            builder.Property(l => l.ErrorCodeRef).HasMaxLength(50);
            builder.Property(l => l.IpAddress).HasMaxLength(50);
            builder.Property(l => l.Result).IsRequired().HasMaxLength(50).HasDefaultValue("Thành công");
            builder.Property(l => l.ErrorMessage).HasMaxLength(1000);

            // Khóa ngoại sang Devices
            builder.HasOne<Device>()
                   .WithMany()
                   .HasForeignKey(l => l.DeviceId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Khóa ngoại sang Users (Nullable)
            builder.HasOne<User>()
                   .WithMany()
                   .HasForeignKey(l => l.UserId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);
        }
    }
}
