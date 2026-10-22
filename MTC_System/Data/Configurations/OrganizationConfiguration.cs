using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>
    /// [Dev 1] Cấu hình bảng Organizations
    /// </summary>
    public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
    {
        public void Configure(EntityTypeBuilder<Organization> builder)
        {
            builder.HasKey(o => o.Id);

            builder.Property(o => o.OrganizationCode)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.HasIndex(o => o.OrganizationCode)
                   .IsUnique(); // Mã tổ chức là duy nhất

            builder.Property(o => o.OrganizationName)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(o => o.TaxCode)
                   .HasMaxLength(20);

            builder.Property(o => o.Address)
                   .HasMaxLength(500);

            builder.Property(o => o.Status)
                   .IsRequired()
                   .HasMaxLength(50)
                   .HasDefaultValue("Còn hạn");
        }
    }
}
