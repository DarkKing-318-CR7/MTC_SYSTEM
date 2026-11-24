namespace MTC_System.DTOs.SharedMedia;

public sealed record CreateSharedMediaRequest(Guid MediaId, Guid ToFacilityId);

public sealed record SharedMediaResponse(
    Guid Id,
    Guid MediaId,
    string MediaName,
    string FileName,
    string FileType,
    long FileSize,
    int DurationSeconds,
    string ApprovalStatus,
    string HlsStatus,
    string? HlsPlaybackUrl,
    string? CategoryName,
    string SourceFacilityName,
    string ReceivingFacilityName,
    string? CreatedByName,
    string? ApprovedByName,
    DateTimeOffset CreatedAt,
    DateTimeOffset SharedAt,
    string Status,
    DateTimeOffset? RespondedAt);
