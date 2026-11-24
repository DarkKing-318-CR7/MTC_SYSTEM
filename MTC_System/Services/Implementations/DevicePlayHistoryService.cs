using Microsoft.EntityFrameworkCore;
using MTC_System.Common;
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
        UserScopeContext context,
        Guid? deviceId,
        Guid? broadcastId,
        string? result,
        CancellationToken cancellationToken)
    {
        context.EnsureCanAccess();

        var query = from hist in _context.DevicePlayHistories.AsNoTracking().Where(x => !x.IsDeleted)
                    join dev in _context.Devices.AsNoTracking().Where(x => !x.IsDeleted) on hist.DeviceId equals dev.Id
                    select new { hist, dev };

        if (!context.IsSystemAdministrator)
        {
            query = query.Where(x => x.dev.OrganizationId == context.OrganizationId!.Value);
            if (context.FacilityId.HasValue)
                query = query.Where(x => x.dev.FacilityId == context.FacilityId.Value);
        }

        if (deviceId.HasValue)
            query = query.Where(x => x.hist.DeviceId == deviceId.Value);
        if (broadcastId.HasValue)
            query = query.Where(x => x.hist.BroadcastId == broadcastId.Value);
        if (!string.IsNullOrWhiteSpace(result))
            query = query.Where(x => x.hist.PlayResult == result.Trim());

        return await query
            .OrderByDescending(x => x.hist.StartedAt)
            .Select(x => new DevicePlayHistoryResponse
            {
                Id = x.hist.Id,
                DeviceId = x.hist.DeviceId,
                BroadcastId = x.hist.BroadcastId,
                BroadcastScheduleId = x.hist.BroadcastScheduleId,
                StartedAt = x.hist.StartedAt,
                EndedAt = x.hist.EndedAt,
                PlayResult = x.hist.PlayResult,
                Notes = x.hist.Notes,
                CreatedAt = x.hist.CreatedAt,
                UpdatedAt = x.hist.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DevicePlayHistoryResponse> GetByIdAsync(Guid id, UserScopeContext context, CancellationToken cancellationToken)
    {
        var item = await (from hist in _context.DevicePlayHistories.AsNoTracking().Where(x => x.Id == id && !x.IsDeleted)
                          join dev in _context.Devices.AsNoTracking().Where(x => !x.IsDeleted) on hist.DeviceId equals dev.Id
                          select new { hist, dev })
                          .FirstOrDefaultAsync(cancellationToken)
                          ?? throw DeviceModuleException.NotFound("Không tìm thấy lịch sử phát của thiết bị.");

        context.ValidateScope(item.dev.OrganizationId, item.dev.FacilityId);

        return new DevicePlayHistoryResponse
        {
            Id = item.hist.Id,
            DeviceId = item.hist.DeviceId,
            BroadcastId = item.hist.BroadcastId,
            BroadcastScheduleId = item.hist.BroadcastScheduleId,
            StartedAt = item.hist.StartedAt,
            EndedAt = item.hist.EndedAt,
            PlayResult = item.hist.PlayResult,
            Notes = item.hist.Notes,
            CreatedAt = item.hist.CreatedAt,
            UpdatedAt = item.hist.UpdatedAt
        };
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
        return ProjectSingle(entity);
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
        return ProjectSingle(entity);
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
                x => x.Id == request.BroadcastScheduleId.Value && x.BroadcastId == request.BroadcastId && !x.IsDeleted,
                cancellationToken);
            if (!scheduleExists)
                throw DeviceModuleException.BadRequest("BroadcastScheduleId không tồn tại hoặc không khớp với bản tin.");
        }
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DevicePlayHistoryResponse ProjectSingle(DevicePlayHistory x) => new()
    {
        Id = x.Id,
        DeviceId = x.DeviceId,
        BroadcastId = x.BroadcastId,
        BroadcastScheduleId = x.BroadcastScheduleId,
        StartedAt = x.StartedAt,
        EndedAt = x.EndedAt,
        PlayResult = x.PlayResult,
        Notes = x.Notes,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };
}
