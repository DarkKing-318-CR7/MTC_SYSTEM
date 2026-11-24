using System.ComponentModel.DataAnnotations;

namespace MTC_System.DTOs.DeviceErrorCodes;

public class CreateDeviceErrorCodeRequest
{
    [Required, StringLength(50)]
    public string ErrorCode { get; set; } = string.Empty;

    [Required, StringLength(255)]
    public string ErrorName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required, StringLength(50)]
    public string Severity { get; set; } = "Trung bình";
}

public class UpdateDeviceErrorCodeRequest : CreateDeviceErrorCodeRequest;

public class DeviceErrorCodeResponse
{
    public Guid Id { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Severity { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
