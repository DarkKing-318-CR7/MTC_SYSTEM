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
    public string HlsProcessingStatus { get; set; } = "NotRequired";
    public string? HlsPlaybackUrl { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? RejectedBy { get; set; }
    public string? RejectedByName { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? Url { get; set; }
    public string? SourceUrl { get; set; }
    public decimal? FrequencyHz { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public IReadOnlyList<string> SharedFacilityNames { get; set; } = Array.Empty<string>();
}
