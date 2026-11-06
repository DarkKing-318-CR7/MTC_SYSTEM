using System.ComponentModel.DataAnnotations;
using MTC_System.Models;

namespace MTC_System.DTOs.Media;

public sealed class UpdateMediaRequest
{
    [Required, StringLength(255, MinimumLength = 1)]
    public string MediaName { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 1)]
    public string FileType { get; set; } = string.Empty;

    [Required]
    public MediaSourceMethod? SourceType { get; set; }

    [Required]
    public Guid FacilityId { get; set; }

    [StringLength(2048)]
    public string? SourceUrl { get; set; }

    [StringLength(2048)]
    public string? Url { get; set; }

    public decimal? FrequencyHz { get; set; }

    public Guid? CategoryId { get; set; }
}
