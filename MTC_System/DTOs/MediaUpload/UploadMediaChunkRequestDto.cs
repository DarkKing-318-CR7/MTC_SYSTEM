using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MTC_System.DTOs.MediaUpload;

public sealed class UploadMediaChunkRequestDto
{
    [Required]
    public IFormFile Chunk { get; set; } = null!;
}