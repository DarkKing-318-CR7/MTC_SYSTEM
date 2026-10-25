using Microsoft.EntityFrameworkCore;
using MTC_System.Data;
using MTC_System.DTOs.DevicePlayHistories;
using MTC_System.Models;
using MTC_System.Services.Interfaces;

namespace MTC_System.Services.Implementations;

public sealed class DevicePlayHistoryService : IDevicePlayHistoryService
{
    private readonly MyDbContext _context;

    public DevicePlayHistoryService(MyDbContext context) => _context = context;

    public async Task<IReadOnlyList<DevicePlayHistoryResponse>> GetAllAsync(
        Guid? deviceId,
        Guid? broadcastId,
        string? result,
        CancellationToken cancellationToken)
    {
        var histories = _context.DevicePlayHistories.AsNoTracking().Where(x => !x.IsDeleted);
        if (deviceId.HasValue)
            histories = histories.Where(x => x.DeviceId == deviceId.Value);
        if (broadcastId.HasValue)
            histories = histories.Where(x => x.BroadcastId == broadcastId.Value);
        if (!string.IsNullOrWhiteSpace(result))
            histories = histories.Where(x => x.PlayResult == result.Trim());

        return await Project(histories).OrderByDescending(x => x.StartedAt).ToListAsync(cancellationToken);
    }

    public async Task<DevicePlayHistoryResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await Project(_context.DevicePlayHistories.AsNoTracking().Where(x => x.Id == id && !x.IsDeleted))
            .FirstOrDefaultAsync(cancellationToken);
        return result ?? throw DeviceModuleException.NotFound("Không tìm thấy lịch sử phát của thiết bị.");
    }

    public async Task<DevicePlayHistoryResponse> CreateAsync(CreateDevicePlayHistoryRequest request, CancellationToken cancellationToken)
    {
        await ValidateRequestAsync(request, cancellationToken);
        var entity = new DevicePlayHistory
        {
            DeviceId = request.DeviceId,
            BroadcastId = request.BroadcastId,
            BroadcastScheduleId = request.BroadcastScheduleId,
            StartedAt = request.StartedAt,
            EndedAt = request.EndedAt,
            PlayResult = request.PlayResult.Trim(),
            Notes = Clean(request.Notes)
        };

        _context.DevicePlayHistories.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<DevicePlayHistoryResponse> UpdateAsync(Guid id, UpdateDevicePlayHistoryRequest request, CancellationToken cancellationToken)
    {
        var entity = await _context.DevicePlayHistories.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw DeviceModuleException.NotFound("Không tìm thấy lịch sử phát của thiết bị.");

        await ValidateRequestAsync(request, cancellationToken);
        entity.DeviceId = request.DeviceId;
        entity.BroadcastId = request.BroadcastId;
        entity.BroadcastScheduleId = request.BroadcastScheduleId;
        entity.StartedAt = request.StartedAt;
        entity.EndedAt = request.EndedAt;
        entity.PlayResult = request.PlayResult.Trim();
        entity.Notes = Clean(request.Notes);
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _context.DevicePlayHistories.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw DeviceModuleException.NotFound("Không tìm thấy lịch sử phát của thiết bị.");
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateRequestAsync(CreateDevicePlayHistoryRequest request, CancellationToken cancellationToken)
    {
        if (request.DeviceId == Guid.Empty || request.BroadcastId == Guid.Empty)
            throw DeviceModuleException.BadRequest("DeviceId và BroadcastId là bắt buộc.");
        if (request.StartedAt == default)
            throw DeviceModuleException.BadRequest("StartedAt là bắt buộc.");
        if (request.EndedAt.HasValue && request.EndedAt.Value < request.StartedAt)
            throw DeviceModuleException.BadRequest("EndedAt phải lớn hơn hoặc bằng StartedAt.");

        var deviceExists = await _context.Devices.AnyAsync(x => x.Id == request.DeviceId && !x.IsDeleted, cancellationToken);
        if (!deviceExists)
            throw DeviceModuleException.BadRequest("DeviceId không tồn tại.");

        var broadcastExists = await _context.Broadcasts.AnyAsync(x => x.Id == request.BroadcastId && !x.IsDeleted, cancellationToken);
        if (!broadcastExists)
            throw DeviceModuleException.BadRequest("BroadcastId không tồn tại.");

        if (request.BroadcastScheduleId.HasValue)
        {
            var scheduleExists = await _context.BroadcastSchedules.AnyAsync(
                x => x.Id == request.BroadcastScheduleId && x.BroadcastId == request.BroadcastId && !x.IsDeleted,
                cancellationToken);
            if (!scheduleExists)
                throw DeviceModuleException.BadRequest("BroadcastScheduleId không tồn tại hoặc không thuộc bản tin đã chọn.");
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private IQueryable<DevicePlayHistoryResponse> Project(IQueryable<DevicePlayHistory> histories) =>
        from history in histories
        join device in _context.Devices.AsNoTracking() on history.DeviceId equals device.Id
        join broadcast in _context.Broadcasts.AsNoTracking() on history.BroadcastId equals broadcast.Id
        select new DevicePlayHistoryResponse
        {
            Id = history.Id,
            DeviceId = history.DeviceId,
            DeviceCode = device.DeviceCode,
            DeviceName = device.DeviceName,
            BroadcastId = history.BroadcastId,
            BroadcastCode = broadcast.BroadcastCode,
            BroadcastName = broadcast.BroadcastName,
            BroadcastScheduleId = history.BroadcastScheduleId,
            StartedAt = history.StartedAt,
            EndedAt = history.EndedAt,
            PlayResult = history.PlayResult,
            Notes = history.Notes,
            CreatedAt = history.CreatedAt,
            UpdatedAt = history.UpdatedAt
        };
}
