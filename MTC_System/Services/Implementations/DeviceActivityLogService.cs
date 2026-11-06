using Microsoft.EntityFrameworkCore;
using MTC_System.Common;
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
        UserScopeContext context,
        Guid? deviceId,
        string? actionType,
        string? result,
        CancellationToken cancellationToken)
    {
        context.EnsureCanAccess();

        var query = from log in _context.DeviceActivityLogs.AsNoTracking().Where(x => !x.IsDeleted)
                    join dev in _context.Devices.AsNoTracking().Where(x => !x.IsDeleted) on log.DeviceId equals dev.Id
                    select new { log, dev };

        if (!context.IsSystemAdministrator)
        {
            query = query.Where(x => x.dev.OrganizationId == context.OrganizationId!.Value);
            if (context.FacilityId.HasValue)
                query = query.Where(x => x.dev.FacilityId == context.FacilityId.Value);
        }

        if (deviceId.HasValue)
            query = query.Where(x => x.log.DeviceId == deviceId.Value);
        if (!string.IsNullOrWhiteSpace(actionType))
            query = query.Where(x => x.log.ActionType == actionType.Trim());
        if (!string.IsNullOrWhiteSpace(result))
            query = query.Where(x => x.log.Result == result.Trim());

        return await query
            .OrderByDescending(x => x.log.CreatedAt)
            .Select(x => new DeviceActivityLogResponse
            {
                Id = x.log.Id,
                DeviceId = x.log.DeviceId,
                UserId = x.log.UserId,
                ActionType = x.log.ActionType,
                ActionDetail = x.log.ActionDetail,
                ErrorCodeRef = x.log.ErrorCodeRef,
                IpAddress = x.log.IpAddress,
                Result = x.log.Result,
                ErrorMessage = x.log.ErrorMessage,
                CreatedAt = x.log.CreatedAt,
                UpdatedAt = x.log.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DeviceActivityLogResponse> GetByIdAsync(Guid id, UserScopeContext context, CancellationToken cancellationToken)
    {
        var item = await (from log in _context.DeviceActivityLogs.AsNoTracking().Where(x => x.Id == id && !x.IsDeleted)
                          join dev in _context.Devices.AsNoTracking().Where(x => !x.IsDeleted) on log.DeviceId equals dev.Id
                          select new { log, dev })
                          .FirstOrDefaultAsync(cancellationToken)
                          ?? throw DeviceModuleException.NotFound("Không tìm thấy nhật ký hoạt động thiết bị.");

        context.ValidateScope(item.dev.OrganizationId, item.dev.FacilityId);

        return new DeviceActivityLogResponse
        {
            Id = item.log.Id,
            DeviceId = item.log.DeviceId,
            UserId = item.log.UserId,
            ActionType = item.log.ActionType,
            ActionDetail = item.log.ActionDetail,
            ErrorCodeRef = item.log.ErrorCodeRef,
            IpAddress = item.log.IpAddress,
            Result = item.log.Result,
            ErrorMessage = item.log.ErrorMessage,
            CreatedAt = item.log.CreatedAt,
            UpdatedAt = item.log.UpdatedAt
        };
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
        return ProjectSingle(entity);
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
        return ProjectSingle(entity);
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
            var code = errorCode.Trim().ToUpperInvariant();
            var codeExists = await _context.DeviceErrorCodes.AnyAsync(x => x.ErrorCode == code && !x.IsDeleted, cancellationToken);
            if (!codeExists)
                throw DeviceModuleException.BadRequest("Mã lỗi không tồn tại trong danh mục.");
        }
    }

    private static string? NormalizeErrorCode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DeviceActivityLogResponse ProjectSingle(DeviceActivityLog x) => new()
    {
        Id = x.Id,
        DeviceId = x.DeviceId,
        UserId = x.UserId,
        ActionType = x.ActionType,
        ActionDetail = x.ActionDetail,
        ErrorCodeRef = x.ErrorCodeRef,
        IpAddress = x.IpAddress,
        Result = x.Result,
        ErrorMessage = x.ErrorMessage,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };
}
