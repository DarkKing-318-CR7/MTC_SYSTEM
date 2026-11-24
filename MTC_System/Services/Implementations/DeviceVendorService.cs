using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using MTC_System.Common;
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
        UserScopeContext context,
        Guid? organizationId,
        string? search,
        CancellationToken cancellationToken)
    {
        context.EnsureCanAccess();

        if (!context.IsSystemAdministrator)
        {
            if (organizationId.HasValue && organizationId.Value != context.OrganizationId!.Value)
                throw DeviceModuleException.Forbidden("Không được phép truy vấn dữ liệu của tổ chức khác (Fail-Closed).");

            organizationId = context.OrganizationId;
        }

        var query = _context.DeviceVendors.AsNoTracking().Where(x => !x.IsDeleted);
        if (organizationId.HasValue)
            query = query.Where(x => x.OrganizationId == null || x.OrganizationId == organizationId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(x => x.VendorName.ToLower().Contains(keyword) || x.VendorCode.ToLower().Contains(keyword));
        }

        var entities = await query.OrderByDescending(x => x.IsDefault).ThenBy(x => x.VendorName)
            .ToListAsync(cancellationToken);
        return entities.Select(Map).ToList();
    }

    public async Task<DeviceVendorResponse> GetByIdAsync(Guid id, UserScopeContext context, CancellationToken cancellationToken)
    {
        var entity = await _context.DeviceVendors.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw DeviceModuleException.NotFound("Không tìm thấy nhà phân phối thiết bị.");

        if (entity.OrganizationId.HasValue)
            context.ValidateScope(entity.OrganizationId.Value);

        return Map(entity);
    }

    public async Task<DeviceVendorResponse> CreateAsync(CreateDeviceVendorRequest request, UserScopeContext context, CancellationToken cancellationToken)
    {
        context.EnsureCanAccess();
        if (!context.IsSystemAdministrator)
        {
            if (!request.OrganizationId.HasValue || request.OrganizationId.Value != context.OrganizationId!.Value)
                throw DeviceModuleException.Forbidden("Bạn chỉ có thể tạo nhà phân phối cho tổ chức của mình (Fail-Closed).");
        }

        await ValidateOrganizationAsync(request.OrganizationId, cancellationToken);
        await EnsureUniqueNameAsync(request.OrganizationId, request.VendorName, null, cancellationToken);
        var vendorCode = NormalizeVendorCode(request.VendorCode);
        await EnsureUniqueCodeAsync(vendorCode, null, cancellationToken);

        var entity = new DeviceVendor
        {
            OrganizationId = request.OrganizationId,
            VendorCode = vendorCode,
            VendorName = request.VendorName.Trim(),
            ContactEmail = request.ContactEmail?.Trim(),
            ContactPhone = request.ContactPhone?.Trim(),
            Website = request.Website?.Trim(),
            Address = request.Address?.Trim(),
            IsDefault = context.IsSystemAdministrator && request.IsDefault
        };

        _context.DeviceVendors.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<DeviceVendorResponse> UpdateAsync(Guid id, UpdateDeviceVendorRequest request, UserScopeContext context, CancellationToken cancellationToken)
    {
        var entity = await _context.DeviceVendors.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw DeviceModuleException.NotFound("Không tìm thấy nhà phân phối thiết bị.");

        if (entity.OrganizationId.HasValue)
            context.ValidateScope(entity.OrganizationId.Value);
        else if (!context.IsSystemAdministrator)
            throw DeviceModuleException.Forbidden("Chỉ Quản trị viên hệ thống mới có quyền sửa nhà phân phối mặc định.");

        await ValidateOrganizationAsync(request.OrganizationId, cancellationToken);
        await EnsureUniqueNameAsync(request.OrganizationId, request.VendorName, id, cancellationToken);
        var vendorCode = NormalizeVendorCode(request.VendorCode);
        await EnsureUniqueCodeAsync(vendorCode, id, cancellationToken);

        entity.OrganizationId = request.OrganizationId;
        entity.VendorCode = vendorCode;
        entity.VendorName = request.VendorName.Trim();
        entity.ContactEmail = request.ContactEmail?.Trim();
        entity.ContactPhone = request.ContactPhone?.Trim();
        entity.Website = request.Website?.Trim();
        entity.Address = request.Address?.Trim();
        if (context.IsSystemAdministrator)
            entity.IsDefault = request.IsDefault;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task DeleteAsync(Guid id, UserScopeContext context, CancellationToken cancellationToken)
    {
        var entity = await _context.DeviceVendors.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw DeviceModuleException.NotFound("Không tìm thấy nhà phân phối thiết bị.");

        if (entity.OrganizationId.HasValue)
            context.ValidateScope(entity.OrganizationId.Value);
        else if (!context.IsSystemAdministrator)
            throw DeviceModuleException.Forbidden("Chỉ Quản trị viên hệ thống mới có quyền xóa nhà phân phối mặc định.");

        if (entity.IsDefault)
            throw DeviceModuleException.BadRequest("Không được xóa nhà phân phối mặc định của hệ thống.");

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

        var exists = await _context.Organizations.AnyAsync(
            x => x.Id == organizationId.Value && !x.IsDeleted,
            cancellationToken);
        if (!exists)
            throw DeviceModuleException.BadRequest("OrganizationId không tồn tại.");
    }

    private async Task EnsureUniqueNameAsync(
        Guid? organizationId,
        string vendorName,
        Guid? currentId,
        CancellationToken cancellationToken)
    {
        var normalized = vendorName.Trim().ToLower();
        var isDuplicate = await _context.DeviceVendors.AnyAsync(
            x => x.OrganizationId == organizationId &&
                 x.VendorName.ToLower() == normalized &&
                 (!currentId.HasValue || x.Id != currentId.Value) &&
                 !x.IsDeleted,
            cancellationToken);

        if (isDuplicate)
            throw DeviceModuleException.Conflict("Tên nhà phân phối đã tồn tại trong phạm vi này.");
    }

    private async Task EnsureUniqueCodeAsync(string vendorCode, Guid? currentId, CancellationToken cancellationToken)
    {
        var normalized = vendorCode.ToLowerInvariant();
        var isDuplicate = await _context.DeviceVendors.AnyAsync(
            x => x.VendorCode.ToLower() == normalized &&
                 (!currentId.HasValue || x.Id != currentId.Value) &&
                 !x.IsDeleted,
            cancellationToken);

        if (isDuplicate)
            throw DeviceModuleException.Conflict("Mã nhà phân phối đã tồn tại.");
    }

    private static string NormalizeVendorCode(string vendorCode)
    {
        var normalized = vendorCode.Trim();
        if (!Regex.IsMatch(normalized, "^[A-Za-z0-9_-]+$"))
            throw DeviceModuleException.BadRequest("Mã nhà phân phối chỉ được chứa chữ, số, dấu gạch ngang hoặc gạch dưới.");
        return normalized;
    }

    private static DeviceVendorResponse Map(DeviceVendor x) => new()
    {
        Id = x.Id,
        OrganizationId = x.OrganizationId,
        VendorCode = x.VendorCode,
        VendorName = x.VendorName,
        ContactEmail = x.ContactEmail,
        ContactPhone = x.ContactPhone,
        Website = x.Website,
        Address = x.Address,
        IsDefault = x.IsDefault,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };
}
