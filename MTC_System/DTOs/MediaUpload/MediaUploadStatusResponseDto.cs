namespace MTC_System.DTOs.MediaUpload;

public sealed class MediaUploadStatusResponseDto
{
    public string UploadIdentifier { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int ReceivedChunks { get; init; }
    public int TotalChunks { get; init; }
    public double ProgressPercentage { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public string? FailureReason { get; init; }
    public Guid? MediaId { get; init; }
}