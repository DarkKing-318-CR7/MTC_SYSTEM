using System.ComponentModel.DataAnnotations;

namespace MTC_System.DTOs.DevicePlayHistories;

public class CreateDevicePlayHistoryRequest
{
    public Guid DeviceId { get; set; }
    public Guid BroadcastId { get; set; }
    public Guid? BroadcastScheduleId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }

    [Required, StringLength(50)]
    public string PlayResult { get; set; } = "Hoàn thành";

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class UpdateDevicePlayHistoryRequest : CreateDevicePlayHistoryRequest;

public class DevicePlayHistoryResponse
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public Guid BroadcastId { get; set; }
    public string BroadcastCode { get; set; } = string.Empty;
    public string BroadcastName { get; set; } = string.Empty;
    public Guid? BroadcastScheduleId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public string PlayResult { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
