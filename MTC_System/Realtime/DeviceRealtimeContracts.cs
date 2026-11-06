using System.Text.Json.Serialization;

namespace MTC_System.Realtime;

public sealed class DeviceCommandEnvelope
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("commandId")]
    public Guid CommandId { get; set; } = Guid.NewGuid();

    [JsonPropertyName("correlationId")]
    public Guid? CorrelationId { get; set; }

    [JsonPropertyName("deviceId")]
    public Guid DeviceId { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("issuedAt")]
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddMinutes(5);

    [JsonPropertyName("payload")]
    public object? Payload { get; set; }
}

public sealed class DeviceHeartbeatRequest
{
    [JsonPropertyName("sentAt")]
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("volume")]
    public int? Volume { get; set; }

    [JsonPropertyName("playStatus")]
    public string? PlayStatus { get; set; }

    [JsonPropertyName("ipAddress")]
    public string? IpAddress { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    [JsonPropertyName("freeStorageBytes")]
    public long? FreeStorageBytes { get; set; }

    [JsonPropertyName("appVersion")]
    public string? AppVersion { get; set; }
}

public sealed class DeviceCommandAckRequest
{
    [JsonPropertyName("commandId")]
    public Guid CommandId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Success"; // Success, Failed, Rejected

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("executedAt")]
    public DateTimeOffset ExecutedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PlaybackReportRequest
{
    [JsonPropertyName("broadcastId")]
    public Guid BroadcastId { get; set; }

    [JsonPropertyName("startedAt")]
    public DateTimeOffset StartedAt { get; set; }

    [JsonPropertyName("endedAt")]
    public DateTimeOffset EndedAt { get; set; }

    [JsonPropertyName("result")]
    public string Result { get; set; } = "Hoàn thành";

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}
