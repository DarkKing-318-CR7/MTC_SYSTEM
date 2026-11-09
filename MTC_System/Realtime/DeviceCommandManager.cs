using System.Collections.Concurrent;

namespace MTC_System.Realtime;

public enum CommandAckResult
{
    Success,
    Duplicate,
    MismatchDevice,
    NotFound
}

public sealed record DeviceCommandState(
    Guid CommandId,
    Guid DeviceId,
    string CommandType,
    string Status, // Pending, Sent, Acknowledged, Failed, Expired
    DateTimeOffset CreatedAt,
    DateTimeOffset? AcknowledgedAt = null,
    string? ErrorMessage = null,
    int? TargetVolume = null);

public interface IDeviceCommandManager
{
    void RegisterCommand(Guid commandId, Guid deviceId, string commandType, string status = "Pending", int? targetVolume = null);
    void MarkSent(Guid commandId);
    CommandAckResult ProcessAck(Guid commandId, Guid deviceId, string status, string? errorMessage);
    DeviceCommandState? GetCommand(Guid commandId);
    IReadOnlyList<DeviceCommandState> GetCommandsForDevice(Guid deviceId);
}

/// <summary>
/// [Architecture Note / Prototype Design]
/// DeviceCommandManager implements an in-memory concurrent store (ConcurrentDictionary)
/// for high-throughput, low-latency realtime command state tracking and idempotent ACK handling.
/// In production multi-node clusters, this should be backed by Redis or a dedicated persistent database table.
/// All command activities and acknowledgements are also durably persisted to PostgreSQL via DeviceActivityLogs.
/// </summary>
public sealed class DeviceCommandManager : IDeviceCommandManager
{
    private readonly ConcurrentDictionary<Guid, DeviceCommandState> _commands = new();

    public void RegisterCommand(Guid commandId, Guid deviceId, string commandType, string status = "Pending", int? targetVolume = null)
    {
        _commands[commandId] = new DeviceCommandState(
            commandId,
            deviceId,
            commandType,
            status,
            DateTimeOffset.UtcNow,
            TargetVolume: targetVolume);
    }

    public void MarkSent(Guid commandId)
    {
        if (_commands.TryGetValue(commandId, out var existing))
        {
            _commands[commandId] = existing with { Status = "Sent" };
        }
    }

    public CommandAckResult ProcessAck(Guid commandId, Guid deviceId, string status, string? errorMessage)
    {
        if (!_commands.TryGetValue(commandId, out var existing))
        {
            // Command does not exist -> return NotFound (Item 6)
            return CommandAckResult.NotFound;
        }

        if (existing.DeviceId != deviceId)
        {
            return CommandAckResult.MismatchDevice;
        }

        if (existing.Status == "Acknowledged" || existing.Status == "Success")
        {
            return CommandAckResult.Duplicate;
        }

        _commands[commandId] = existing with
        {
            Status = status,
            AcknowledgedAt = DateTimeOffset.UtcNow,
            ErrorMessage = errorMessage
        };

        return CommandAckResult.Success;
    }

    public DeviceCommandState? GetCommand(Guid commandId) =>
        _commands.TryGetValue(commandId, out var state) ? state : null;

    public IReadOnlyList<DeviceCommandState> GetCommandsForDevice(Guid deviceId) =>
        _commands.Values.Where(x => x.DeviceId == deviceId).OrderByDescending(x => x.CreatedAt).ToList();
}
