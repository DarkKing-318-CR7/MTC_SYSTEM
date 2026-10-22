using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 3] Cấu hình bảng BroadcastTimeSlots</summary>
    public class BroadcastTimeSlotConfiguration : IEntityTypeConfiguration<BroadcastTimeSlot>
    {
        public void Configure(EntityTypeBuilder<BroadcastTimeSlot> builder)
        {
            builder.HasKey(t => t.Id);

            // Khóa ngoại sang BroadcastSchedules
            builder.HasOne<BroadcastSchedule>()
                   .WithMany(s => s.TimeSlots)
                   .HasForeignKey(t => t.BroadcastScheduleId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
