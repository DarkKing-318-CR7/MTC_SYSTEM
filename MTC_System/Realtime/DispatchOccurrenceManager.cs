using System.Collections.Concurrent;

namespace MTC_System.Realtime;

public interface IDispatchOccurrenceManager
{
    bool IsDispatched(string occurrenceKey);
    bool TryClaim(string occurrenceKey);
    bool Release(string occurrenceKey);
    void MarkDispatched(string occurrenceKey);
    IReadOnlyCollection<string> GetDispatchedOccurrences();
}

/// <summary>
/// Quản lý khóa Idempotency cho các lần điều phối bản tin (occurrence) đến từng thiết bị.
/// Đảm bảo mỗi khung giờ/ngày (occurrence) của bản tin chỉ được bắn lệnh đến thiết bị đúng 1 lần duy nhất.
/// </summary>
public sealed class DispatchOccurrenceManager : IDispatchOccurrenceManager
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _dispatchedOccurrences = new();

    public bool IsDispatched(string occurrenceKey) =>
        _dispatchedOccurrences.ContainsKey(occurrenceKey);

    public bool TryClaim(string occurrenceKey) =>
        _dispatchedOccurrences.TryAdd(occurrenceKey, DateTimeOffset.UtcNow);

    public bool Release(string occurrenceKey) =>
        _dispatchedOccurrences.TryRemove(occurrenceKey, out _);

    public void MarkDispatched(string occurrenceKey) =>
        _dispatchedOccurrences[occurrenceKey] = DateTimeOffset.UtcNow;

    public IReadOnlyCollection<string> GetDispatchedOccurrences() =>
        _dispatchedOccurrences.Keys.ToList();
}
