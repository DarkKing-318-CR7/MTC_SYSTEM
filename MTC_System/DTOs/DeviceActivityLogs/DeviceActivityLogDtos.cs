using System.ComponentModel.DataAnnotations;

namespace MTC_System.DTOs.DeviceActivityLogs;

public class CreateDeviceActivityLogRequest
{
    public Guid DeviceId { get; set; }
    public Guid? UserId { get; set; }

    [Required, StringLength(50)]
    public string ActionType { get; set; } = string.Empty;

    public string? ActionDetail { get; set; }

    [StringLength(50)]
    public string? ErrorCodeRef { get; set; }

    [StringLength(50)]
    public string? IpAddress { get; set; }

    [Required, StringLength(50)]
    public string Result { get; set; } = "Thành công";

    [StringLength(1000)]
    public string? ErrorMessage { get; set; }
}

public class UpdateDeviceActivityLogRequest : CreateDeviceActivityLogRequest;

public class DeviceActivityLogResponse
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? ActionDetail { get; set; }
    public string? ErrorCodeRef { get; set; }
    public string? IpAddress { get; set; }
    public string Result { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
