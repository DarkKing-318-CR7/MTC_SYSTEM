using Microsoft.EntityFrameworkCore;
using MTC_System.Data;
using MTC_System.DTOs.DeviceActivityLogs;
using MTC_System.Models;
using MTC_System.Services.Interfaces;

namespace MTC_System.Services.Implementations;

public sealed class DeviceActivityLogService : IDeviceActivityLogService
{
    private readonly MyDbContext _context;

    public DeviceActivityLogService(MyDbContext context) => _context = context;

    public async Task<IReadOnlyList<DeviceActivityLogResponse>> GetAllAsync(
        Guid? deviceId,
        string? actionType,
        string? result,
        CancellationToken cancellationToken)
    {
        var logs = _context.DeviceActivityLogs.AsNoTracking().Where(x => !x.IsDeleted);
        if (deviceId.HasValue)
            logs = logs.Where(x => x.DeviceId == deviceId.Value);
        if (!string.IsNullOrWhiteSpace(actionType))
            logs = logs.Where(x => x.ActionType == actionType.Trim());
        if (!string.IsNullOrWhiteSpace(result))
            logs = logs.Where(x => x.Result == result.Trim());

        return await Project(logs).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<DeviceActivityLogResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await Project(_context.DeviceActivityLogs.AsNoTracking().Where(x => x.Id == id && !x.IsDeleted))
            .FirstOrDefaultAsync(cancellationToken);
        return result ?? throw DeviceModuleException.NotFound("Không tìm thấy nhật ký hoạt động thiết bị.");
    }

    public async Task<DeviceActivityLogResponse> CreateAsync(CreateDeviceActivityLogRequest request, CancellationToken cancellationToken)
    {
        await ValidateReferencesAsync(request.DeviceId, request.UserId, request.ErrorCodeRef, cancellationToken);

        var entity = new DeviceActivityLog
        {
            DeviceId = request.DeviceId,
            UserId = request.UserId,
            ActionType = request.ActionType.Trim(),
            ActionDetail = Clean(request.ActionDetail),
            ErrorCodeRef = NormalizeErrorCode(request.ErrorCodeRef),
            IpAddress = Clean(request.IpAddress),
            Result = request.Result.Trim(),
            ErrorMessage = Clean(request.ErrorMessage)
        };

        _context.DeviceActivityLogs.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<DeviceActivityLogResponse> UpdateAsync(Guid id, UpdateDeviceActivityLogRequest request, CancellationToken cancellationToken)
    {
        var entity = await _context.DeviceActivityLogs.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw DeviceModuleException.NotFound("Không tìm thấy nhật ký hoạt động thiết bị.");

        await ValidateReferencesAsync(request.DeviceId, request.UserId, request.ErrorCodeRef, cancellationToken);
        entity.DeviceId = request.DeviceId;
        entity.UserId = request.UserId;
        entity.ActionType = request.ActionType.Trim();
        entity.ActionDetail = Clean(request.ActionDetail);
        entity.ErrorCodeRef = NormalizeErrorCode(request.ErrorCodeRef);
        entity.IpAddress = Clean(request.IpAddress);
        entity.Result = request.Result.Trim();
        entity.ErrorMessage = Clean(request.ErrorMessage);
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _context.DeviceActivityLogs.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw DeviceModuleException.NotFound("Không tìm thấy nhật ký hoạt động thiết bị.");
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateReferencesAsync(Guid deviceId, Guid? userId, string? errorCode, CancellationToken cancellationToken)
    {
        if (deviceId == Guid.Empty)
            throw DeviceModuleException.BadRequest("DeviceId là bắt buộc.");
        var deviceExists = await _context.Devices.AnyAsync(x => x.Id == deviceId && !x.IsDeleted, cancellationToken);
        if (!deviceExists)
            throw DeviceModuleException.BadRequest("DeviceId không tồn tại.");

        if (userId.HasValue)
        {
            var userExists = await _context.Users.AnyAsync(x => x.Id == userId && !x.IsDeleted, cancellationToken);
            if (!userExists)
                throw DeviceModuleException.BadRequest("UserId không tồn tại.");
        }

        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            var normalized = errorCode.Trim().ToUpperInvariant();
            var errorExists = await _context.DeviceErrorCodes.AnyAsync(
                x => x.ErrorCode == normalized && !x.IsDeleted,
                cancellationToken);
            if (!errorExists)
                throw DeviceModuleException.BadRequest("ErrorCodeRef không tồn tại.");
        }
    }

    private static string? NormalizeErrorCode(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private IQueryable<DeviceActivityLogResponse> Project(IQueryable<DeviceActivityLog> logs) =>
        from log in logs
        join device in _context.Devices.AsNoTracking() on log.DeviceId equals device.Id
        select new DeviceActivityLogResponse
        {
            Id = log.Id,
            DeviceId = log.DeviceId,
            DeviceCode = device.DeviceCode,
            DeviceName = device.DeviceName,
            UserId = log.UserId,
            ActionType = log.ActionType,
            ActionDetail = log.ActionDetail,
            ErrorCodeRef = log.ErrorCodeRef,
            IpAddress = log.IpAddress,
            Result = log.Result,
            ErrorMessage = log.ErrorMessage,
            CreatedAt = log.CreatedAt,
            UpdatedAt = log.UpdatedAt
        };
}
