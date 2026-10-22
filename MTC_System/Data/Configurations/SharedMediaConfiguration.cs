using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 2] Cấu hình bảng SharedMedia</summary>
    public class SharedMediaConfiguration : IEntityTypeConfiguration<SharedMedia>
    {
        public void Configure(EntityTypeBuilder<SharedMedia> builder)
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Status).HasMaxLength(50).HasDefaultValue("Chờ tiếp nhận");

            // Mỗi lần chia sẻ 1 media từ 1 cơ sở đến 1 cơ sở là duy nhất
            builder.HasIndex(s => new { s.FromFacilityId, s.ToFacilityId, s.MediaId }).IsUnique();

            builder.HasOne<Facility>()
                   .WithMany()
                   .HasForeignKey(s => s.FromFacilityId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Facility>()
                   .WithMany()
                   .HasForeignKey(s => s.ToFacilityId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Media>()
                   .WithMany()
                   .HasForeignKey(s => s.MediaId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Organization>()
                   .WithMany()
                   .HasForeignKey(s => s.OrganizationId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
