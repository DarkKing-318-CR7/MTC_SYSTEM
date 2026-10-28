using Microsoft.EntityFrameworkCore;
using MTC_System.Data;
using MTC_System.DTOs.DeviceGroups;
using MTC_System.Models;
using MTC_System.Services.Interfaces;

namespace MTC_System.Services.Implementations;

public sealed class DeviceGroupService : IDeviceGroupService
{
    private readonly MyDbContext _context;

    public DeviceGroupService(MyDbContext context) => _context = context;

    public async Task<IReadOnlyList<DeviceGroupResponse>> GetAllAsync(
        Guid? organizationId,
        Guid? facilityId,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = _context.DeviceGroups.AsNoTracking().Where(x => !x.IsDeleted);

        if (organizationId.HasValue)
            query = query.Where(x => x.OrganizationId == organizationId.Value);
        if (facilityId.HasValue)
            query = query.Where(x => x.FacilityId == facilityId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(x => x.GroupName.ToLower().Contains(keyword) ||
                                     (x.Description != null && x.Description.ToLower().Contains(keyword)));
        }

        return await query
            .OrderBy(x => x.GroupName)
            .Select(x => new DeviceGroupResponse
            {
                Id = x.Id,
                OrganizationId = x.OrganizationId,
                FacilityId = x.FacilityId,
                GroupName = x.GroupName,
                Description = x.Description,
                IsActive = x.IsActive,
                DeviceCount = x.Devices.Count(d => !d.IsDeleted),
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DeviceGroupResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _context.DeviceGroups
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new DeviceGroupResponse
            {
                Id = x.Id,
                OrganizationId = x.OrganizationId,
                FacilityId = x.FacilityId,
                GroupName = x.GroupName,
                Description = x.Description,
                IsActive = x.IsActive,
                DeviceCount = x.Devices.Count(d => !d.IsDeleted),
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return result ?? throw DeviceModuleException.NotFound("Không tìm thấy nhóm thiết bị.");
    }

    public async Task<DeviceGroupResponse> CreateAsync(CreateDeviceGroupRequest request, CancellationToken cancellationToken)
    {
        await ValidateScopeAsync(request.OrganizationId, request.FacilityId, cancellationToken);
        await EnsureUniqueNameAsync(request.GroupName, request.OrganizationId, request.FacilityId, null, cancellationToken);

        var entity = new DeviceGroup
        {
            OrganizationId = request.OrganizationId,
            FacilityId = request.FacilityId,
            GroupName = request.GroupName.Trim(),
            Description = request.Description?.Trim(),
            IsActive = request.IsActive
        };

        _context.DeviceGroups.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<DeviceGroupResponse> UpdateAsync(Guid id, UpdateDeviceGroupRequest request, CancellationToken cancellationToken)
    {
        var entity = await _context.DeviceGroups.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw DeviceModuleException.NotFound("Không tìm thấy nhóm thiết bị.");

        await ValidateScopeAsync(request.OrganizationId, request.FacilityId, cancellationToken);
        await EnsureUniqueNameAsync(request.GroupName, request.OrganizationId, request.FacilityId, id, cancellationToken);

        entity.OrganizationId = request.OrganizationId;
        entity.FacilityId = request.FacilityId;
        entity.GroupName = request.GroupName.Trim();
        entity.Description = request.Description?.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _context.DeviceGroups.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw DeviceModuleException.NotFound("Không tìm thấy nhóm thiết bị.");

        var devices = await _context.Devices
            .Where(x => x.DeviceGroupId == id && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var device in devices)
        {
            device.DeviceGroupId = null;
            device.UpdatedAt = DateTimeOffset.UtcNow;
        }

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateScopeAsync(Guid organizationId, Guid facilityId, CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || facilityId == Guid.Empty)
            throw DeviceModuleException.BadRequest("OrganizationId và FacilityId là bắt buộc.");

        var organizationExists = await _context.Organizations.AnyAsync(x => x.Id == organizationId && !x.IsDeleted, cancellationToken);
        if (!organizationExists)
            throw DeviceModuleException.BadRequest("OrganizationId không tồn tại.");

        var facilityExists = await _context.Facilities.AnyAsync(
            x => x.Id == facilityId && x.OrganizationId == organizationId && !x.IsDeleted,
            cancellationToken);
        if (!facilityExists)
            throw DeviceModuleException.BadRequest("FacilityId không tồn tại hoặc không thuộc tổ chức đã chọn.");
    }

    private async Task EnsureUniqueNameAsync(
        string name,
        Guid organizationId,
        Guid facilityId,
        Guid? ignoredId,
        CancellationToken cancellationToken)
    {
        var normalized = name.Trim().ToLower();
        var duplicated = await _context.DeviceGroups.AnyAsync(
            x => !x.IsDeleted &&
                 x.OrganizationId == organizationId &&
                 x.FacilityId == facilityId &&
                 x.GroupName.ToLower() == normalized &&
                 (!ignoredId.HasValue || x.Id != ignoredId.Value),
            cancellationToken);

        if (duplicated)
            throw DeviceModuleException.Conflict("Tên nhóm thiết bị đã tồn tại trong cơ sở.");
    }
}
