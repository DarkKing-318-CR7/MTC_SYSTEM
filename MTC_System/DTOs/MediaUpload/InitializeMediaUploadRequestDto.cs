using System.ComponentModel.DataAnnotations;

namespace MTC_System.DTOs.MediaUpload;

public sealed class InitializeMediaUploadRequestDto
{
    public const long MaximumFileSize = 1024L * 1024 * 1024;

    [Required, StringLength(255, MinimumLength = 1)]
    public string FileName { get; set; } = string.Empty;

    [Required, StringLength(255, MinimumLength = 1)]
    public string MediaName { get; set; } = string.Empty;

    [Range(1, MaximumFileSize)]
    public long FileSize { get; set; }

    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    public Guid FacilityId { get; set; }

    public Guid? CategoryId { get; set; }
}