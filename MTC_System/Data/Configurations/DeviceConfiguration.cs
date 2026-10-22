using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 4] Cấu hình bảng Devices (Thiết bị màn hình / cụm loa)</summary>
    public class DeviceConfiguration : IEntityTypeConfiguration<Device>
    {
        public void Configure(EntityTypeBuilder<Device> builder)
        {
            builder.HasKey(d => d.Id);

            builder.Property(d => d.DeviceCode).IsRequired().HasMaxLength(50);
            builder.HasIndex(d => d.DeviceCode).IsUnique();

            builder.Property(d => d.DeviceName).IsRequired().HasMaxLength(255);
            builder.Property(d => d.SerialNumber).HasMaxLength(100);
            builder.Property(d => d.IpAddress).HasMaxLength(50);
            builder.Property(d => d.SimPhoneNumber).HasMaxLength(20);
            builder.Property(d => d.SimSerialNumber).HasMaxLength(50);
            builder.Property(d => d.Tag).HasMaxLength(100);
            builder.Property(d => d.VendorName).HasMaxLength(100);
            builder.Property(d => d.OsType).IsRequired().HasMaxLength(50).HasDefaultValue("Android");
            builder.Property(d => d.ConnectionStatus).IsRequired().HasMaxLength(50).HasDefaultValue("Mất kết nối");
            builder.Property(d => d.PlayStatus).IsRequired().HasMaxLength(50).HasDefaultValue("Dừng phát bản tin");
            builder.Property(d => d.ScreenshotUrl).HasMaxLength(500);
            builder.Property(d => d.InstallationAddress).HasMaxLength(500);
            builder.Property(d => d.ActivationQrToken).HasMaxLength(500);
            builder.Property(d => d.ActivationOtp).HasMaxLength(10);

            // Khóa ngoại sang Facilities
            builder.HasOne<Facility>()
                   .WithMany()
                   .HasForeignKey(d => d.FacilityId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Khóa ngoại sang Organizations
            builder.HasOne<Organization>()
                   .WithMany()
                   .HasForeignKey(d => d.OrganizationId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Khóa ngoại sang DeviceGroups (Xóa nhóm thì thiết bị chuyển DeviceGroupId = null)
            builder.HasOne(d => d.DeviceGroup)
                   .WithMany(g => g.Devices)
                   .HasForeignKey(d => d.DeviceGroupId)
                   .OnDelete(DeleteBehavior.SetNull)
                   .IsRequired(false);
        }
    }
}
