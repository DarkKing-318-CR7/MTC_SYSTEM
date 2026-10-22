using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>
    /// [Dev 1] Cấu hình bảng Users
    /// </summary>
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);

            builder.Property(u => u.Username)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.HasIndex(u => u.Username)
                   .IsUnique(); // Tên đăng nhập là duy nhất

            builder.Property(u => u.PasswordHash)
                   .IsRequired()
                   .HasMaxLength(500);

            builder.Property(u => u.FullName)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(u => u.Email)
                   .HasMaxLength(255);

            builder.HasIndex(u => u.Email)
                   .IsUnique()
                   .HasFilter("\"Email\" IS NOT NULL AND \"Email\" != ''"); // Email duy nhất nhưng có thể để trống

            builder.Property(u => u.PhoneNumber)
                   .HasMaxLength(20);

            // Khóa ngoại trỏ sang Facilities: Cho phép null (System User và Org Admin = null)
            builder.Property(u => u.FacilityId)
                   .IsRequired(false);

            builder.HasOne<Facility>()
                   .WithMany()
                   .HasForeignKey(u => u.FacilityId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);

            // Khóa ngoại trỏ sang Organizations: Cho phép null (System User = null)
            builder.Property(u => u.OrganizationId)
                   .IsRequired(false);

            builder.HasOne<Organization>()
                   .WithMany()
                   .HasForeignKey(u => u.OrganizationId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);

            // Cấu hình các trường 2FA
            builder.Property(u => u.TwoFactorSecretKey)
                   .HasMaxLength(255);

            builder.Property(u => u.BackupCodesJson)
                   .HasColumnType("text");
        }
    }
}
