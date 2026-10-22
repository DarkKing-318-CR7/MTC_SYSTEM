using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 2] Cấu hình bảng Templates</summary>
    public class TemplateConfiguration : IEntityTypeConfiguration<Template>
    {
        public void Configure(EntityTypeBuilder<Template> builder)
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.TemplateName).IsRequired().HasMaxLength(255);
            builder.Property(t => t.Description).HasMaxLength(1000);
            builder.Property(t => t.LayoutConfigJson).HasColumnType("text");

            builder.HasOne<Facility>()
                   .WithMany()
                   .HasForeignKey(t => t.FacilityId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Organization>()
                   .WithMany()
                   .HasForeignKey(t => t.OrganizationId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
