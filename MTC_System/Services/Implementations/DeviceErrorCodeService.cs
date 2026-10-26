using Microsoft.EntityFrameworkCore;
using MTC_System.Data;
using MTC_System.DTOs.DeviceErrorCodes;
using MTC_System.Models;
using MTC_System.Services.Interfaces;

namespace MTC_System.Services.Implementations;

public sealed class DeviceErrorCodeService : IDeviceErrorCodeService
{
    private readonly MyDbContext _context;

    public DeviceErrorCodeService(MyDbContext context) => _context = context;

    public async Task<IReadOnlyList<DeviceErrorCodeResponse>> GetAllAsync(string? search, string? severity, CancellationToken cancellationToken)
    {
        var query = _context.DeviceErrorCodes.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(x => x.ErrorCode.ToLower().Contains(keyword) || x.ErrorName.ToLower().Contains(keyword));
        }
        if (!string.IsNullOrWhiteSpace(severity))
            query = query.Where(x => x.Severity == severity.Trim());

        var entities = await query.OrderBy(x => x.ErrorCode).ToListAsync(cancellationToken);
        return entities.Select(Map).ToList();
    }

    public async Task<DeviceErrorCodeResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _context.DeviceErrorCodes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw DeviceModuleException.NotFound("Không tìm thấy mã lỗi thiết bị.");
        return Map(entity);
    }

    public async Task<DeviceErrorCodeResponse> CreateAsync(CreateDeviceErrorCodeRequest request, CancellationToken cancellationToken)
    {
        var code = request.ErrorCode.Trim().ToUpperInvariant();
        await EnsureUniqueCodeAsync(code, null, cancellationToken);

        var entity = new DeviceErrorCode
        {
            ErrorCode = code,
            ErrorName = request.ErrorName.Trim(),
            Description = request.Description?.Trim(),
            Severity = request.Severity.Trim()
        };
        _context.DeviceErrorCodes.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<DeviceErrorCodeResponse> UpdateAsync(Guid id, UpdateDeviceErrorCodeRequest request, CancellationToken cancellationToken)
    {
        var entity = await _context.DeviceErrorCodes.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw DeviceModuleException.NotFound("Không tìm thấy mã lỗi thiết bị.");

        var code = request.ErrorCode.Trim().ToUpperInvariant();
        await EnsureUniqueCodeAsync(code, id, cancellationToken);
        entity.ErrorCode = code;
        entity.ErrorName = request.ErrorName.Trim();
        entity.Description = request.Description?.Trim();
        entity.Severity = request.Severity.Trim();
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _context.DeviceErrorCodes.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw DeviceModuleException.NotFound("Không tìm thấy mã lỗi thiết bị.");
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureUniqueCodeAsync(string code, Guid? ignoredId, CancellationToken cancellationToken)
    {
        var duplicated = await _context.DeviceErrorCodes.AnyAsync(
            x => x.ErrorCode == code && (!ignoredId.HasValue || x.Id != ignoredId.Value),
            cancellationToken);
        if (duplicated)
            throw DeviceModuleException.Conflict("Mã lỗi thiết bị đã tồn tại.");
    }

    private static DeviceErrorCodeResponse Map(DeviceErrorCode entity) => new()
    {
        Id = entity.Id,
        ErrorCode = entity.ErrorCode,
        ErrorName = entity.ErrorName,
        Description = entity.Description,
        Severity = entity.Severity,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
