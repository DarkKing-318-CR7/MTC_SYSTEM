using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 5] Cấu hình bảng SystemNotifications</summary>
    public class SystemNotificationConfiguration : IEntityTypeConfiguration<SystemNotification>
    {
        public void Configure(EntityTypeBuilder<SystemNotification> builder)
        {
            builder.HasKey(n => n.Id);

            builder.Property(n => n.Title).IsRequired().HasMaxLength(255);
            builder.Property(n => n.Content).IsRequired().HasColumnType("text");
            builder.Property(n => n.NotificationType).IsRequired().HasMaxLength(50);
            builder.Property(n => n.TargetOrganizationIds).HasColumnType("text");

            // Khóa ngoại sang Organizations (Nullable)
            builder.Property(n => n.OrganizationId).IsRequired(false);
            builder.HasOne<Organization>()
                   .WithMany()
                   .HasForeignKey(n => n.OrganizationId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);

            // Khóa ngoại sang Facilities (Nullable)
            builder.Property(n => n.FacilityId).IsRequired(false);
            builder.HasOne<Facility>()
                   .WithMany()
                   .HasForeignKey(n => n.FacilityId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);

            // Khóa ngoại sang Users (TargetUserId - Nullable)
            builder.HasOne<User>()
                   .WithMany()
                   .HasForeignKey(n => n.TargetUserId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);
        }
    }
}
