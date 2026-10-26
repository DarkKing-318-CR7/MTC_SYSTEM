using Microsoft.EntityFrameworkCore;
using MTC_System.Data;
using MTC_System.DTOs.Devices;
using MTC_System.Models;
using MTC_System.Services.Interfaces;

namespace MTC_System.Services.Implementations;

public sealed class DeviceService : IDeviceService
{
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
        Guid? organizationId,
        Guid? facilityId,
        string? search,
        CancellationToken cancellationToken)
    {
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

    public async Task<DeviceResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await Project(_context.Devices.AsNoTracking().Where(x => x.Id == id && !x.IsDeleted))
            .FirstOrDefaultAsync(cancellationToken);
        return result ?? throw DeviceModuleException.NotFound("Không tìm thấy thiết bị.");
    }

    public async Task<DeviceResponse> CreateAsync(CreateDeviceRequest request, CancellationToken cancellationToken)
    {
        await ValidateRequestAsync(request, null, cancellationToken);

        var entity = new Device
        {
            OrganizationId = request.OrganizationId,
            FacilityId = request.FacilityId,
            DeviceCode = request.DeviceCode.Trim().ToUpperInvariant(),
            DeviceName = request.DeviceName.Trim(),
            SerialNumber = Clean(request.SerialNumber),
            IpAddress = Clean(request.IpAddress),
            SimPhoneNumber = Clean(request.SimPhoneNumber),
            SimSerialNumber = Clean(request.SimSerialNumber),
            Tag = Clean(request.Tag),
            OsType = NormalizeOsType(request.OsType),
            VendorName = Clean(request.VendorName),
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
        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<DeviceResponse> UpdateAsync(Guid id, UpdateDeviceRequest request, CancellationToken cancellationToken)
    {
        var entity = await _context.Devices.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw DeviceModuleException.NotFound("Không tìm thấy thiết bị.");

        await ValidateRequestAsync(request, id, cancellationToken);
        ValidateStatuses(request.ConnectionStatus, request.PlayStatus);

        entity.OrganizationId = request.OrganizationId;
        entity.FacilityId = request.FacilityId;
        entity.DeviceCode = request.DeviceCode.Trim().ToUpperInvariant();
        entity.DeviceName = request.DeviceName.Trim();
        entity.SerialNumber = Clean(request.SerialNumber);
        entity.IpAddress = Clean(request.IpAddress);
        entity.SimPhoneNumber = Clean(request.SimPhoneNumber);
        entity.SimSerialNumber = Clean(request.SimSerialNumber);
        entity.Tag = Clean(request.Tag);
        entity.OsType = NormalizeOsType(request.OsType);
        entity.VendorName = Clean(request.VendorName);
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
        entity.IsActivated = request.IsActivated;
        entity.LastOnlineAt = request.LastOnlineAt;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _context.Devices.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw DeviceModuleException.NotFound("Không tìm thấy thiết bị.");

        entity.IsDeleted = true;
        entity.ActiveStatus = false;
        entity.ConnectionStatus = "Mất kết nối";
        entity.PlayStatus = "Dừng phát bản tin";
        entity.ActivationQrToken = null;
        entity.ActivationOtp = null;
        entity.ActivationTokenExpiry = null;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
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
        var value = osType.Trim();
        return AllowedOsTypes.First(x => x.Equals(value, StringComparison.OrdinalIgnoreCase));
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IQueryable<DeviceResponse> Project(IQueryable<Device> query) => query.Select(x => new DeviceResponse
    {
        Id = x.Id,
        OrganizationId = x.OrganizationId,
        FacilityId = x.FacilityId,
        DeviceCode = x.DeviceCode,
        DeviceName = x.DeviceName,
        SerialNumber = x.SerialNumber,
        IpAddress = x.IpAddress,
        SimPhoneNumber = x.SimPhoneNumber,
        SimSerialNumber = x.SimSerialNumber,
        Tag = x.Tag,
        OsType = x.OsType,
        VendorName = x.VendorName,
        DeviceGroupId = x.DeviceGroupId,
        DeviceGroupName = x.DeviceGroup != null && !x.DeviceGroup.IsDeleted ? x.DeviceGroup.GroupName : null,
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
        IsActivated = x.IsActivated,
        HasPendingActivation = x.ActivationQrToken != null || x.ActivationOtp != null,
        ActivationTokenExpiry = x.ActivationTokenExpiry,
        LastOnlineAt = x.LastOnlineAt,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    });
}
