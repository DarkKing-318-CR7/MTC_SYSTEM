using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 5] Cấu hình bảng AuditLogs (Nhật ký biến động dữ liệu)</summary>
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Action).IsRequired().HasMaxLength(50);
            builder.Property(a => a.EntityName).IsRequired().HasMaxLength(100);
            builder.Property(a => a.EntityId).HasMaxLength(100);
            builder.Property(a => a.OldValueJson).HasColumnType("text");
            builder.Property(a => a.NewValueJson).HasColumnType("text");
            builder.Property(a => a.IpAddress).HasMaxLength(50);
            builder.Property(a => a.UserAgent).HasMaxLength(500);

            // Khóa ngoại sang Organizations (Nullable)
            builder.Property(a => a.OrganizationId).IsRequired(false);
            builder.HasOne<Organization>()
                   .WithMany()
                   .HasForeignKey(a => a.OrganizationId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);

            // Khóa ngoại sang Facilities (Nullable)
            builder.Property(a => a.FacilityId).IsRequired(false);
            builder.HasOne<Facility>()
                   .WithMany()
                   .HasForeignKey(a => a.FacilityId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);

            // Khóa ngoại sang Users (Nullable)
            builder.HasOne<User>()
                   .WithMany()
                   .HasForeignKey(a => a.UserId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);
        }
    }
}
