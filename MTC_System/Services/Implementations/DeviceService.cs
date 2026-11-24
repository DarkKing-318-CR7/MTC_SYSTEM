using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using MTC_System.Common;
using MTC_System.Data;
using MTC_System.DTOs.Devices;
using MTC_System.Models;
using MTC_System.Services.Interfaces;

namespace MTC_System.Services.Implementations;

public sealed class DeviceService : IDeviceService
{
    private static readonly Guid DefaultVendorId = Guid.Parse("dddddddd-0001-0000-0000-000000000000");
    private static readonly HashSet<string> AllowedOsTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Android", "Linux", "Windows"
    };

    private static readonly HashSet<string> AllowedConnectionStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Đang kết nối", "Mất kết nối"
    };

    private static readonly HashSet<string> AllowedPlayStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Đang phát bản tin", "Dừng phát bản tin"
    };

    private readonly MyDbContext _context;

    public DeviceService(MyDbContext context) => _context = context;

    public async Task<IReadOnlyList<DeviceResponse>> GetAllAsync(
        UserScopeContext context,
        Guid? organizationId,
        Guid? facilityId,
        string? search,
        CancellationToken cancellationToken)
    {
        context.EnsureCanAccess();

        if (!context.IsSystemAdministrator)
        {
            if (organizationId.HasValue && organizationId.Value != context.OrganizationId!.Value)
                throw DeviceModuleException.Forbidden("Không được phép truy vấn dữ liệu của tổ chức khác (Fail-Closed).");

            organizationId = context.OrganizationId;

            if (context.FacilityId.HasValue)
            {
                if (facilityId.HasValue && facilityId.Value != context.FacilityId.Value)
                    throw DeviceModuleException.Forbidden("Không được phép truy vấn dữ liệu của cơ sở khác (Fail-Closed).");

                facilityId = context.FacilityId;
            }
        }

        var query = _context.Devices.AsNoTracking().Where(x => !x.IsDeleted);
        if (organizationId.HasValue)
            query = query.Where(x => x.OrganizationId == organizationId.Value);
        if (facilityId.HasValue)
            query = query.Where(x => x.FacilityId == facilityId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(x =>
                x.DeviceCode.ToLower().Contains(keyword) ||
                x.DeviceName.ToLower().Contains(keyword) ||
                (x.SerialNumber != null && x.SerialNumber.ToLower().Contains(keyword)) ||
                (x.IpAddress != null && x.IpAddress.ToLower().Contains(keyword)));
        }

        return await Project(query)
            .OrderBy(x => x.DeviceName)
            .ToListAsync(cancellationToken);
    }

    public async Task<DeviceResponse> GetByIdAsync(Guid id, UserScopeContext context, CancellationToken cancellationToken)
    {
        var device = await _context.Devices.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw DeviceModuleException.NotFound("Không tìm thấy thiết bị.");

        context.ValidateScope(device.OrganizationId, device.FacilityId);

        var result = await Project(_context.Devices.AsNoTracking().Where(x => x.Id == id && !x.IsDeleted))
            .FirstOrDefaultAsync(cancellationToken);
        return result!;
    }

    public async Task<IReadOnlyList<DeviceMapItemResponse>> GetMapAsync(
        UserScopeContext context,
        Guid? organizationId,
        Guid? facilityId,
        CancellationToken cancellationToken)
    {
        context.EnsureCanAccess();

        if (!context.IsSystemAdministrator)
        {
            if (organizationId.HasValue && organizationId.Value != context.OrganizationId!.Value)
                throw DeviceModuleException.Forbidden("Không được phép truy vấn bản đồ của tổ chức khác (Fail-Closed).");

            organizationId = context.OrganizationId;
            if (context.FacilityId.HasValue)
            {
                if (facilityId.HasValue && facilityId.Value != context.FacilityId.Value)
                    throw DeviceModuleException.Forbidden("Không được phép truy vấn bản đồ của cơ sở khác (Fail-Closed).");
                facilityId = context.FacilityId;
            }
        }

        var query = _context.Devices.AsNoTracking().Where(x => !x.IsDeleted);
        if (organizationId.HasValue)
            query = query.Where(x => x.OrganizationId == organizationId.Value);
        if (facilityId.HasValue)
            query = query.Where(x => x.FacilityId == facilityId.Value);

        return await query
            .OrderBy(x => x.DeviceName)
            .Select(x => new DeviceMapItemResponse
            {
                DeviceId = x.Id,
                DeviceCode = x.DeviceCode,
                DeviceName = x.DeviceName,
                OrganizationId = x.OrganizationId,
                FacilityId = x.FacilityId,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                InstallationAddress = x.InstallationAddress,
                ConnectionStatus = x.ConnectionStatus,
                PlayStatus = x.PlayStatus,
                LastOnlineAt = x.LastOnlineAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DeviceResponse> CreateAsync(CreateDeviceRequest request, UserScopeContext context, CancellationToken cancellationToken)
    {
        context.EnsureCanAccess();
        context.ValidateScope(request.OrganizationId, request.FacilityId);

        await ValidateRequestAsync(request, null, cancellationToken);
        var vendor = await ResolveVendorAsync(request.OrganizationId, request.DeviceVendorId, request.VendorName, cancellationToken);

        var entity = new Device
        {
            OrganizationId = request.OrganizationId,
            FacilityId = request.FacilityId,
            DeviceCode = request.DeviceCode.Trim().ToUpperInvariant(),
            DeviceName = request.DeviceName.Trim(),
            Description = Clean(request.Description),
            SerialNumber = Clean(request.SerialNumber),
            IpAddress = Clean(request.IpAddress),
            SimPhoneNumber = Clean(request.SimPhoneNumber),
            SimSerialNumber = Clean(request.SimSerialNumber),
            Tag = Clean(request.Tag),
            OsType = NormalizeOsType(request.OsType),
            DeviceVendorId = vendor.Id,
            VendorName = vendor.VendorName,
            DeviceGroupId = request.DeviceGroupId,
            ActiveStatus = request.ActiveStatus,
            ConnectionStatus = "Mất kết nối",
            PlayStatus = "Dừng phát bản tin",
            Volume = request.Volume,
            ScreenshotUrl = Clean(request.ScreenshotUrl),
            ScreenshotSizeInBytes = request.ScreenshotSizeInBytes,
            LastScreenshotAt = request.LastScreenshotAt,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            InstallationAddress = Clean(request.InstallationAddress),
            IsActivated = false
        };

        _context.Devices.Add(entity);
        _context.DeviceActivityLogs.Add(CreateCrudLog(entity.Id, context.UserId, "DeviceCreated", new
        {
            entity.DeviceCode,
            entity.DeviceName,
            entity.FacilityId,
            entity.DeviceVendorId
        }));
        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(entity.Id, context, cancellationToken);
    }

    public async Task<DeviceResponse> UpdateAsync(Guid id, UpdateDeviceRequest request, UserScopeContext context, CancellationToken cancellationToken)
    {
        var entity = await _context.Devices.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw DeviceModuleException.NotFound("Không tìm thấy thiết bị.");

        context.ValidateScope(entity.OrganizationId, entity.FacilityId);

        if (request.OrganizationId != entity.OrganizationId)
            throw DeviceModuleException.BadRequest("Không được phép thay đổi OrganizationId của thiết bị.");

        context.ValidateScope(request.OrganizationId, request.FacilityId);

        await ValidateRequestAsync(request, id, cancellationToken);
        var vendor = await ResolveVendorAsync(request.OrganizationId, request.DeviceVendorId, request.VendorName, cancellationToken);
        ValidateStatuses(request.ConnectionStatus, request.PlayStatus);
        var isBeingLocked = entity.ActiveStatus && !request.ActiveStatus;
        var before = new
        {
            entity.DeviceCode,
            entity.DeviceName,
            entity.FacilityId,
            entity.DeviceVendorId,
            entity.ActiveStatus,
            entity.Volume
        };

        entity.FacilityId = request.FacilityId;
        entity.DeviceCode = request.DeviceCode.Trim().ToUpperInvariant();
        entity.DeviceName = request.DeviceName.Trim();
        entity.Description = Clean(request.Description);
        entity.SerialNumber = Clean(request.SerialNumber);
        entity.IpAddress = Clean(request.IpAddress);
        entity.SimPhoneNumber = Clean(request.SimPhoneNumber);
        entity.SimSerialNumber = Clean(request.SimSerialNumber);
        entity.Tag = Clean(request.Tag);
        entity.OsType = NormalizeOsType(request.OsType);
        entity.DeviceVendorId = vendor.Id;
        entity.VendorName = vendor.VendorName;
        entity.DeviceGroupId = request.DeviceGroupId;
        entity.ActiveStatus = request.ActiveStatus;
        entity.ConnectionStatus = request.ConnectionStatus.Trim();
        entity.PlayStatus = request.PlayStatus.Trim();
        entity.Volume = request.Volume;
        entity.ScreenshotUrl = Clean(request.ScreenshotUrl);
        entity.ScreenshotSizeInBytes = request.ScreenshotSizeInBytes;
        entity.LastScreenshotAt = request.LastScreenshotAt;
        entity.Latitude = request.Latitude;
        entity.Longitude = request.Longitude;
        entity.InstallationAddress = Clean(request.InstallationAddress);
        entity.LastOnlineAt = request.LastOnlineAt;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        if (isBeingLocked)
            RevokeDeviceCredentials(entity);

        _context.DeviceActivityLogs.Add(CreateCrudLog(entity.Id, context.UserId, "DeviceUpdated", new
        {
            before,
            after = new
            {
                entity.DeviceCode,
                entity.DeviceName,
                entity.FacilityId,
                entity.DeviceVendorId,
                entity.ActiveStatus,
                entity.Volume
            }
        }));
        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, context, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, UserScopeContext context, CancellationToken cancellationToken)
    {
        var entity = await _context.Devices.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw DeviceModuleException.NotFound("Không tìm thấy thiết bị.");

        context.ValidateScope(entity.OrganizationId, entity.FacilityId);

        entity.IsDeleted = true;
        entity.ActiveStatus = false;
        entity.ConnectionStatus = "Mất kết nối";
        entity.PlayStatus = "Dừng phát bản tin";
        RevokeDeviceCredentials(entity);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        _context.DeviceActivityLogs.Add(CreateCrudLog(entity.Id, context.UserId, "DeviceDeleted", new
        {
            entity.DeviceCode,
            entity.DeviceName
        }));
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateRequestAsync(CreateDeviceRequest request, Guid? ignoredId, CancellationToken cancellationToken)
    {
        if (request.OrganizationId == Guid.Empty || request.FacilityId == Guid.Empty)
            throw DeviceModuleException.BadRequest("OrganizationId và FacilityId là bắt buộc.");
        if (!AllowedOsTypes.Contains(request.OsType.Trim()))
            throw DeviceModuleException.BadRequest("OsType chỉ nhận Android, Linux hoặc Windows.");

        var organizationExists = await _context.Organizations.AnyAsync(
            x => x.Id == request.OrganizationId && !x.IsDeleted,
            cancellationToken);
        if (!organizationExists)
            throw DeviceModuleException.BadRequest("OrganizationId không tồn tại.");

        var facilityExists = await _context.Facilities.AnyAsync(
            x => x.Id == request.FacilityId && x.OrganizationId == request.OrganizationId && !x.IsDeleted,
            cancellationToken);
        if (!facilityExists)
            throw DeviceModuleException.BadRequest("FacilityId không tồn tại hoặc không thuộc tổ chức đã chọn.");

        var normalizedCode = request.DeviceCode.Trim().ToUpperInvariant();
        var duplicateCode = await _context.Devices.AnyAsync(
            x => x.DeviceCode == normalizedCode && (!ignoredId.HasValue || x.Id != ignoredId.Value),
            cancellationToken);
        if (duplicateCode)
            throw DeviceModuleException.Conflict("DeviceCode đã tồn tại.");

        if (request.DeviceGroupId.HasValue)
        {
            var groupExists = await _context.DeviceGroups.AnyAsync(
                x => x.Id == request.DeviceGroupId &&
                     x.OrganizationId == request.OrganizationId &&
                     x.FacilityId == request.FacilityId &&
                     !x.IsDeleted,
                cancellationToken);
            if (!groupExists)
                throw DeviceModuleException.BadRequest("Nhóm thiết bị không tồn tại hoặc không cùng tổ chức/cơ sở.");
        }
    }

    private static void ValidateStatuses(string connectionStatus, string playStatus)
    {
        if (!AllowedConnectionStatuses.Contains(connectionStatus.Trim()))
            throw DeviceModuleException.BadRequest("ConnectionStatus không hợp lệ.");
        if (!AllowedPlayStatuses.Contains(playStatus.Trim()))
            throw DeviceModuleException.BadRequest("PlayStatus không hợp lệ.");
    }

    private static string NormalizeOsType(string osType)
    {
        var trimmed = osType.Trim();
        if (trimmed.Equals("Android", StringComparison.OrdinalIgnoreCase)) return "Android";
        if (trimmed.Equals("Linux", StringComparison.OrdinalIgnoreCase)) return "Linux";
        if (trimmed.Equals("Windows", StringComparison.OrdinalIgnoreCase)) return "Windows";
        throw DeviceModuleException.BadRequest("OsType chỉ nhận Android, Linux hoặc Windows.");
    }

    private static void RevokeDeviceCredentials(Device entity)
    {
        entity.ActivationOtpHash = null;
        entity.ActivationQrTokenHash = null;
        entity.ActivationCodeExpiresAt = null;
        entity.DeviceRefreshTokenHash = null;
        entity.DeviceRefreshTokenExpiresAt = null;
        entity.DeviceTokenVersion++;
    }

    private async Task<DeviceVendor> ResolveVendorAsync(
        Guid organizationId,
        Guid? vendorId,
        string? vendorName,
        CancellationToken cancellationToken)
    {
        DeviceVendor? vendor;
        if (vendorId.HasValue)
        {
            vendor = await _context.DeviceVendors.FirstOrDefaultAsync(
                x => x.Id == vendorId.Value && !x.IsDeleted &&
                     (!x.OrganizationId.HasValue || x.OrganizationId == organizationId),
                cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(vendorName))
        {
            var normalizedName = vendorName.Trim().ToLower();
            vendor = await _context.DeviceVendors.FirstOrDefaultAsync(
                x => x.VendorName.ToLower() == normalizedName && !x.IsDeleted &&
                     (!x.OrganizationId.HasValue || x.OrganizationId == organizationId),
                cancellationToken);
        }
        else
        {
            vendor = await _context.DeviceVendors.FirstOrDefaultAsync(
                x => x.Id == DefaultVendorId && !x.IsDeleted,
                cancellationToken);
        }

        return vendor ?? throw DeviceModuleException.BadRequest(
            "Nhà phân phối không tồn tại, đã bị xóa hoặc không thuộc tổ chức của thiết bị.");
    }

    private static DeviceActivityLog CreateCrudLog(Guid deviceId, Guid? userId, string actionType, object detail) => new()
    {
        DeviceId = deviceId,
        UserId = userId,
        ActionType = actionType,
        Result = "Thành công",
        ActionDetail = JsonSerializer.Serialize(detail)
    };

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IQueryable<DeviceResponse> Project(IQueryable<Device> query) =>
        query.Select(x => new DeviceResponse
        {
            Id = x.Id,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            OrganizationId = x.OrganizationId,
            FacilityId = x.FacilityId,
            DeviceCode = x.DeviceCode,
            DeviceName = x.DeviceName,
            Description = x.Description,
            SerialNumber = x.SerialNumber,
            IpAddress = x.IpAddress,
            SimPhoneNumber = x.SimPhoneNumber,
            SimSerialNumber = x.SimSerialNumber,
            Tag = x.Tag,
            OsType = x.OsType,
            VendorName = x.VendorName,
            DeviceVendorId = x.DeviceVendorId,
            VendorCode = x.DeviceVendor == null ? null : x.DeviceVendor.VendorCode,
            DeviceGroupId = x.DeviceGroupId,
            DeviceGroupName = x.DeviceGroup == null ? null : x.DeviceGroup.GroupName,
            ActiveStatus = x.ActiveStatus,
            ConnectionStatus = x.ConnectionStatus,
            PlayStatus = x.PlayStatus,
            Volume = x.Volume,
            ScreenshotUrl = x.ScreenshotUrl,
            ScreenshotSizeInBytes = x.ScreenshotSizeInBytes,
            LastScreenshotAt = x.LastScreenshotAt,
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            InstallationAddress = x.InstallationAddress,
            LastOnlineAt = x.LastOnlineAt,
            IsActivated = x.IsActivated,
            ActivatedAt = x.ActivatedAt
        });
}
