using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>
    /// [Dev 1] Cấu hình bảng Facilities (Cây cơ sở cha - con tự tham chiếu)
    /// </summary>
    public class FacilityConfiguration : IEntityTypeConfiguration<Facility>
    {
        public void Configure(EntityTypeBuilder<Facility> builder)
        {
            builder.HasKey(f => f.Id);

            builder.Property(f => f.FacilityCode)
                   .IsRequired()
                   .HasMaxLength(50);

            // Mã cơ sở là duy nhất trong toàn hệ thống
            builder.HasIndex(f => f.FacilityCode)
                   .IsUnique();

            builder.Property(f => f.FacilityName)
                   .IsRequired()
                   .HasMaxLength(255);

            // Liên kết Tổ chức: Bắt buộc thuộc về 1 Organization
            builder.Property(f => f.OrganizationId)
                   .IsRequired();

            builder.HasOne<Organization>()
                   .WithMany()
                   .HasForeignKey(f => f.OrganizationId)
                   .OnDelete(DeleteBehavior.Restrict);

            // CÂY TỰ THAM CHIẾU CHA - CON:
            // Cơ sở cấp cao nhất: ParentId = null (Root)
            // Khi cơ sở cha bị xóa: KHÔNG xóa dây chuyền, chặn lại (Restrict)
            builder.HasOne<Facility>()
                   .WithMany()
                   .HasForeignKey(f => f.ParentId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);
        }
    }
}
