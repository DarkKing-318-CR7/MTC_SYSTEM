using System;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// Tracks the lifecycle and metadata of a chunked media upload session.
    /// </summary>
    public class MediaUploadSession : BaseEntity
    {
        /// <summary>Public identifier for the upload session (unique, required).</summary>
        public string UploadIdentifier { get; set; } = string.Empty;

        /// <summary>Status: Uploading, Finalizing, Completed, Failed, Expired.</summary>
        public string Status { get; set; } = "Uploading";

        /// <summary>Original file name (required).</summary>
        public string ExpectedFileName { get; set; } = string.Empty;

        /// <summary>Expected file size in bytes (required).</summary>
        public long ExpectedFileSize { get; set; }

        /// <summary>Chunk size in bytes (required).</summary>
        public int ChunkSize { get; set; }

        /// <summary>Total number of chunks (required).</summary>
        public int TotalChunks { get; set; }

        /// <summary>Bitmap tracking received chunks (required).</summary>
        public byte[] ReceivedChunksBitmap { get; set; } = Array.Empty<byte>();

        /// <summary>Temporary file path for assembly (required).</summary>
        public string TempFilePath { get; set; } = string.Empty;

        /// <summary>MD5 checksum (set on completion).</summary>
        public string? ChecksumMd5 { get; set; }

        /// <summary>SHA256 checksum (set on completion).</summary>
        public string? ChecksumSha256 { get; set; }

        /// <summary>Expiration timestamp for cleanup (optional).</summary>
        public DateTimeOffset? ExpiresAt { get; set; }

        /// <summary>Completion timestamp (optional).</summary>
        public DateTimeOffset? CompletedAt { get; set; }

        /// <summary>Failure reason (optional).</summary>
        public string? FailedReason { get; set; }

        /// <summary>FK to Media (optional, set only after successful completion).</summary>
        public Guid? MediaId { get; set; }
        public virtual Media? Media { get; set; }
    }
}