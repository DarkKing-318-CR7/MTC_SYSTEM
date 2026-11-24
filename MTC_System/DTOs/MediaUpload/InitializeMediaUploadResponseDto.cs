namespace MTC_System.DTOs.MediaUpload;

public sealed class InitializeMediaUploadResponseDto
{
    public string UploadIdentifier { get; init; } = string.Empty;
    public int ChunkSize { get; init; }
    public int TotalChunks { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
}