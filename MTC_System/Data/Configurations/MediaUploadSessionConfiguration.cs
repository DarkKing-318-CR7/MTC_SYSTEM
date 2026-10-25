using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for MediaUploadSession.
    /// </summary>
    public class MediaUploadSessionConfiguration : IEntityTypeConfiguration<MediaUploadSession>
    {
        public void Configure(EntityTypeBuilder<MediaUploadSession> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.UploadIdentifier).IsRequired().HasMaxLength(100);
            builder.HasIndex(x => x.UploadIdentifier).IsUnique();

            builder.Property(x => x.Status).IsRequired().HasMaxLength(50);
            builder.Property(x => x.ExpectedFileName).IsRequired().HasMaxLength(255);
            builder.Property(x => x.ExpectedFileSize).IsRequired();
            builder.Property(x => x.ChunkSize).IsRequired();
            builder.Property(x => x.TotalChunks).IsRequired();
            builder.Property(x => x.ReceivedChunksBitmap).IsRequired();
            builder.Property(x => x.TempFilePath).IsRequired().HasMaxLength(500);
            builder.Property(x => x.ChecksumMd5).HasMaxLength(50);
            builder.Property(x => x.ChecksumSha256).HasMaxLength(100);
            builder.Property(x => x.FailedReason).HasMaxLength(500);

            // FK to Media (optional, set only after successful completion)
            builder.HasOne(x => x.Media)
                   .WithMany()
                   .HasForeignKey(x => x.MediaId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}