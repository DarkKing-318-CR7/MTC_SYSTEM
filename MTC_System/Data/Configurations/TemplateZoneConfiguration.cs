using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 2] Cấu hình bảng TemplateZones</summary>
    public class TemplateZoneConfiguration : IEntityTypeConfiguration<TemplateZone>
    {
        public void Configure(EntityTypeBuilder<TemplateZone> builder)
        {
            builder.HasKey(z => z.Id);
            builder.Property(z => z.ZoneName).IsRequired().HasMaxLength(100);
            builder.Property(z => z.AllowedMediaTypes).HasMaxLength(200);
            builder.Property(z => z.WidthPercent).HasPrecision(5, 2);
            builder.Property(z => z.HeightPercent).HasPrecision(5, 2);

            // Khóa ngoại sang Templates
            builder.HasOne<Template>()
                   .WithMany(t => t.Zones)
                   .HasForeignKey(z => z.TemplateId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
