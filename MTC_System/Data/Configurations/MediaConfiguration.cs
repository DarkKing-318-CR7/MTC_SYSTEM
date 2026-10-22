using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 2] Cấu hình bảng Media</summary>
    public class MediaConfiguration : IEntityTypeConfiguration<Media>
    {
        public void Configure(EntityTypeBuilder<Media> builder)
        {
            builder.HasKey(m => m.Id);

            builder.Property(m => m.MediaName).IsRequired().HasMaxLength(255);
            builder.Property(m => m.FileName).IsRequired().HasMaxLength(500);
            builder.Property(m => m.FilePath).HasMaxLength(1000);
            builder.Property(m => m.FileType).IsRequired().HasMaxLength(50);
            builder.Property(m => m.ApprovalStatus).IsRequired().HasMaxLength(50).HasDefaultValue("Chờ duyệt");
            builder.Property(m => m.RejectionReason).HasMaxLength(510);
            builder.Property(m => m.ChecksumMd5).HasMaxLength(50);
            builder.Property(m => m.ChecksumSha256).HasMaxLength(100);
            builder.Property(m => m.Url).HasMaxLength(1000);
            builder.Property(m => m.FrequencyHz).HasMaxLength(50);

            // Khóa ngoại sang Facilities
            builder.HasOne<Facility>()
                   .WithMany()
                   .HasForeignKey(m => m.FacilityId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Khóa ngoại sang Organizations
            builder.HasOne<Organization>()
                   .WithMany()
                   .HasForeignKey(m => m.OrganizationId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Khóa ngoại sang MediaCategories (SetNull khi xóa danh mục)
            builder.HasOne(m => m.Category)
                   .WithMany(c => c.MediaList)
                   .HasForeignKey(m => m.CategoryId)
                   .OnDelete(DeleteBehavior.SetNull)
                   .IsRequired(false);
        }
    }
}
