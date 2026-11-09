using System.Collections.Concurrent;

namespace MTC_System.Realtime;

public interface IDeviceConnectionManager
{
    void RegisterConnection(Guid deviceId, string connectionId);
    void UnregisterConnection(string connectionId);
    bool TryGetConnectionId(Guid deviceId, out string? connectionId);
    bool IsOnline(Guid deviceId);
    IReadOnlyCollection<Guid> GetOnlineDeviceIds();
}

public sealed class DeviceConnectionManager : IDeviceConnectionManager
{
    private readonly ConcurrentDictionary<Guid, string> _deviceToConnection = new();
    private readonly ConcurrentDictionary<string, Guid> _connectionToDevice = new();

    public void RegisterConnection(Guid deviceId, string connectionId)
    {
        // If device had old connection, remove it
        if (_deviceToConnection.TryRemove(deviceId, out var oldConnectionId))
        {
            _connectionToDevice.TryRemove(oldConnectionId, out _);
        }

        _deviceToConnection[deviceId] = connectionId;
        _connectionToDevice[connectionId] = deviceId;
    }

    public void UnregisterConnection(string connectionId)
    {
        if (_connectionToDevice.TryRemove(connectionId, out var deviceId))
        {
            _deviceToConnection.TryRemove(deviceId, out _);
        }
    }

    public bool TryGetConnectionId(Guid deviceId, out string? connectionId) =>
        _deviceToConnection.TryGetValue(deviceId, out connectionId);

    public bool IsOnline(Guid deviceId) => _deviceToConnection.ContainsKey(deviceId);

    public IReadOnlyCollection<Guid> GetOnlineDeviceIds() => _deviceToConnection.Keys.ToList();
}
