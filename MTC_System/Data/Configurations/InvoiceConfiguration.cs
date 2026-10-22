using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 5] Cấu hình bảng Invoices (Hóa đơn thanh toán)</summary>
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.HasKey(i => i.Id);

            builder.Property(i => i.InvoiceCode).IsRequired().HasMaxLength(50);
            builder.HasIndex(i => i.InvoiceCode).IsUnique();

            builder.Property(i => i.Amount).HasPrecision(18, 2);
            builder.Property(i => i.Discount).HasPrecision(18, 2);
            builder.Property(i => i.AmountDue).HasPrecision(18, 2);
            builder.Property(i => i.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Chờ thanh toán");
            builder.Property(i => i.TransactionType).IsRequired().HasMaxLength(50).HasDefaultValue("Đăng ký mới");
            builder.Property(i => i.PaymentMethod).HasMaxLength(50);
            builder.Property(i => i.Notes).HasMaxLength(1000);

            // Liên kết Tổ chức
            builder.HasOne<Organization>()
                   .WithMany()
                   .HasForeignKey(i => i.OrganizationId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Khóa ngoại sang Subscriptions
            builder.HasOne<Subscription>()
                   .WithMany()
                   .HasForeignKey(i => i.SubscriptionId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
