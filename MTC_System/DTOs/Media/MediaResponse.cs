using MTC_System.Models;

namespace MTC_System.DTOs.Media;

public sealed class MediaResponse
{
    public Guid Id { get; set; }
    public string MediaName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public MediaSourceMethod SourceType { get; set; }
    public long FileSize { get; set; }
    public int DurationSeconds { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public string? Url { get; set; }
    public string? SourceUrl { get; set; }
    public decimal? FrequencyHz { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
