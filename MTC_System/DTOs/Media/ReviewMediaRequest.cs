using System.ComponentModel.DataAnnotations;

namespace MTC_System.DTOs.Media;

public sealed class ReviewMediaRequest
{
    [StringLength(510)]
    public string? RejectionReason { get; set; }
}
