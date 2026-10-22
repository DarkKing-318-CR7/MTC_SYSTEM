using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>
    /// [Dev 1] Cấu hình bảng RefreshTokens (Quản lý phiên đăng nhập JWT)
    /// </summary>
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(rt => rt.Id);

            builder.Property(rt => rt.Token)
                   .IsRequired()
                   .HasMaxLength(500);

            builder.HasIndex(rt => rt.Token)
                   .IsUnique();

            // Khóa ngoại sang Users: Xóa mềm User thì token vô hiệu (xử lý ở Service)
            builder.HasOne<User>()
                   .WithMany()
                   .HasForeignKey(rt => rt.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
