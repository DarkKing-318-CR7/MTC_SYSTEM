namespace MTC_System.DTOs.MediaUpload;

public sealed class CompleteMediaUploadResponseDto
{
    public Guid MediaId { get; init; }
    public string Status { get; init; } = "Completed";
    public long FileSize { get; init; }
    public string ChecksumMd5 { get; init; } = string.Empty;
    public string ChecksumSha256 { get; init; } = string.Empty;
}