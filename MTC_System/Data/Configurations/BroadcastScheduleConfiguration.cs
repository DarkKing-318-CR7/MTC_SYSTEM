using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 3] Cấu hình bảng BroadcastSchedules</summary>
    public class BroadcastScheduleConfiguration : IEntityTypeConfiguration<BroadcastSchedule>
    {
        public void Configure(EntityTypeBuilder<BroadcastSchedule> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.RepeatType).IsRequired().HasMaxLength(50).HasDefaultValue("Không lặp");
            builder.Property(s => s.RepeatDays).HasMaxLength(100);

            // Khóa ngoại sang Broadcasts
            builder.HasOne<Broadcast>()
                   .WithMany(b => b.Schedules)
                   .HasForeignKey(s => s.BroadcastId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
