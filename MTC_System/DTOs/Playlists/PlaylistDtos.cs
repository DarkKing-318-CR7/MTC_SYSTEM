using System.ComponentModel.DataAnnotations;

namespace MTC_System.DTOs.Playlists;

public sealed class PlaylistUpsertRequest
{
    public Guid? OrganizationId { get; set; }
    [Required] public Guid FacilityId { get; set; }
    [Required, StringLength(255)] public string PlaylistName { get; set; } = string.Empty;
    [StringLength(1000)] public string? Description { get; set; }
    [Required, StringLength(50)] public string PlayMode { get; set; } = "Theo thứ tự";
    public List<PlaylistItemUpsertRequest> Items { get; set; } = [];
}

public sealed class PlaylistItemUpsertRequest
{
    [Required] public Guid MediaId { get; set; }
    [Range(0, int.MaxValue)] public int DurationSeconds { get; set; }
}

public sealed class PlaylistResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid FacilityId { get; set; }
    public string PlaylistName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string PlayMode { get; set; } = string.Empty;
    public int TotalDurationSeconds { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public IReadOnlyList<PlaylistItemResponse> Items { get; set; } = [];
}

public sealed class PlaylistItemResponse
{
    public Guid Id { get; set; }
    public Guid MediaId { get; set; }
    public string MediaName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int DurationSeconds { get; set; }
    public int MediaDurationSeconds { get; set; }
}
