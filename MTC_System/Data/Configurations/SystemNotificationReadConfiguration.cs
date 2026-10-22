using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 5] Cấu hình bảng SystemNotificationReads</summary>
    public class SystemNotificationReadConfiguration : IEntityTypeConfiguration<SystemNotificationRead>
    {
        public void Configure(EntityTypeBuilder<SystemNotificationRead> builder)
        {
            builder.HasKey(r => r.Id);

            // Mỗi User chỉ có 1 bản ghi đọc cho mỗi Notification
            builder.HasIndex(r => new { r.NotificationId, r.UserId }).IsUnique();

            // Khóa ngoại sang SystemNotifications
            builder.HasOne<SystemNotification>()
                   .WithMany()
                   .HasForeignKey(r => r.NotificationId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Khóa ngoại sang Users
            builder.HasOne<User>()
                   .WithMany()
                   .HasForeignKey(r => r.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
