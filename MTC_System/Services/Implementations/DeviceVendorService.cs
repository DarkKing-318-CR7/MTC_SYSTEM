using Microsoft.EntityFrameworkCore;
using MTC_System.Data;
using MTC_System.DTOs.DeviceVendors;
using MTC_System.Models;
using MTC_System.Services.Interfaces;

namespace MTC_System.Services.Implementations;

public sealed class DeviceVendorService : IDeviceVendorService
{
    private readonly MyDbContext _context;

    public DeviceVendorService(MyDbContext context) => _context = context;

    public async Task<IReadOnlyList<DeviceVendorResponse>> GetAllAsync(
        Guid? organizationId,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = _context.DeviceVendors.AsNoTracking().Where(x => !x.IsDeleted);
        if (organizationId.HasValue)
            query = query.Where(x => x.OrganizationId == null || x.OrganizationId == organizationId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(x => x.VendorName.ToLower().Contains(keyword));
        }

        var entities = await query.OrderByDescending(x => x.IsDefault).ThenBy(x => x.VendorName)
            .ToListAsync(cancellationToken);
        return entities.Select(Map).ToList();
    }

    public async Task<DeviceVendorResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _context.DeviceVendors.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw DeviceModuleException.NotFound("Không tìm thấy nhà phân phối thiết bị.");
        return Map(entity);
    }

    public async Task<DeviceVendorResponse> CreateAsync(CreateDeviceVendorRequest request, CancellationToken cancellationToken)
    {
        await ValidateOrganizationAsync(request.OrganizationId, cancellationToken);
        await EnsureUniqueNameAsync(request.OrganizationId, request.VendorName, null, cancellationToken);

        var entity = new DeviceVendor
        {
            OrganizationId = request.OrganizationId,
            VendorName = request.VendorName.Trim(),
            ContactEmail = request.ContactEmail?.Trim(),
            ContactPhone = request.ContactPhone?.Trim(),
            Website = request.Website?.Trim(),
            Address = request.Address?.Trim(),
            IsDefault = request.IsDefault
        };

        _context.DeviceVendors.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<DeviceVendorResponse> UpdateAsync(Guid id, UpdateDeviceVendorRequest request, CancellationToken cancellationToken)
    {
        var entity = await _context.DeviceVendors.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw DeviceModuleException.NotFound("Không tìm thấy nhà phân phối thiết bị.");

        await ValidateOrganizationAsync(request.OrganizationId, cancellationToken);
        await EnsureUniqueNameAsync(request.OrganizationId, request.VendorName, id, cancellationToken);

        entity.OrganizationId = request.OrganizationId;
        entity.VendorName = request.VendorName.Trim();
        entity.ContactEmail = request.ContactEmail?.Trim();
        entity.ContactPhone = request.ContactPhone?.Trim();
        entity.Website = request.Website?.Trim();
        entity.Address = request.Address?.Trim();
        entity.IsDefault = request.IsDefault;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _context.DeviceVendors.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw DeviceModuleException.NotFound("Không tìm thấy nhà phân phối thiết bị.");

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateOrganizationAsync(Guid? organizationId, CancellationToken cancellationToken)
    {
        if (!organizationId.HasValue)
            return;
        if (organizationId == Guid.Empty)
            throw DeviceModuleException.BadRequest("OrganizationId không hợp lệ.");

        var exists = await _context.Organizations.AnyAsync(x => x.Id == organizationId && !x.IsDeleted, cancellationToken);
        if (!exists)
            throw DeviceModuleException.BadRequest("OrganizationId không tồn tại.");
    }

    private async Task EnsureUniqueNameAsync(Guid? organizationId, string name, Guid? ignoredId, CancellationToken cancellationToken)
    {
        var normalized = name.Trim().ToLower();
        var duplicated = await _context.DeviceVendors.AnyAsync(
            x => !x.IsDeleted &&
                 x.OrganizationId == organizationId &&
                 x.VendorName.ToLower() == normalized &&
                 (!ignoredId.HasValue || x.Id != ignoredId.Value),
            cancellationToken);
        if (duplicated)
            throw DeviceModuleException.Conflict("Tên nhà phân phối đã tồn tại trong phạm vi tổ chức.");
    }

    private static DeviceVendorResponse Map(DeviceVendor entity) => new()
    {
        Id = entity.Id,
        OrganizationId = entity.OrganizationId,
        VendorName = entity.VendorName,
        ContactEmail = entity.ContactEmail,
        ContactPhone = entity.ContactPhone,
        Website = entity.Website,
        Address = entity.Address,
        IsDefault = entity.IsDefault,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
