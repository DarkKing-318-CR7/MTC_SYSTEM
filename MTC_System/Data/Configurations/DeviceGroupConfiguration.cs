using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 4] Cấu hình bảng DeviceGroups (Nhóm thiết bị)</summary>
    public class DeviceGroupConfiguration : IEntityTypeConfiguration<DeviceGroup>
    {
        public void Configure(EntityTypeBuilder<DeviceGroup> builder)
        {
            builder.HasKey(g => g.Id);

            builder.Property(g => g.GroupName).IsRequired().HasMaxLength(255);
            builder.Property(g => g.Description).HasMaxLength(1000);

            // Khóa ngoại sang Facilities
            builder.HasOne<Facility>()
                   .WithMany()
                   .HasForeignKey(g => g.FacilityId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Khóa ngoại sang Organizations
            builder.HasOne<Organization>()
                   .WithMany()
                   .HasForeignKey(g => g.OrganizationId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
