using MTC_System.Common;
using MTC_System.DTOs.DeviceActivityLogs;
using MTC_System.DTOs.DeviceErrorCodes;
using MTC_System.DTOs.DeviceGroups;
using MTC_System.DTOs.DevicePlayHistories;
using MTC_System.DTOs.Devices;
using MTC_System.DTOs.DeviceVendors;

namespace MTC_System.Services.Interfaces;

public interface IDeviceService
{
    Task<IReadOnlyList<DeviceResponse>> GetAllAsync(UserScopeContext context, Guid? organizationId, Guid? facilityId, string? search, CancellationToken cancellationToken);
    Task<IReadOnlyList<DeviceMapItemResponse>> GetMapAsync(UserScopeContext context, Guid? organizationId, Guid? facilityId, CancellationToken cancellationToken);
    Task<DeviceResponse> GetByIdAsync(Guid id, UserScopeContext context, CancellationToken cancellationToken);
    Task<DeviceResponse> CreateAsync(CreateDeviceRequest request, UserScopeContext context, CancellationToken cancellationToken);
    Task<DeviceResponse> UpdateAsync(Guid id, UpdateDeviceRequest request, UserScopeContext context, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, UserScopeContext context, CancellationToken cancellationToken);
}

public interface IDeviceGroupService
{
    Task<IReadOnlyList<DeviceGroupResponse>> GetAllAsync(UserScopeContext context, Guid? organizationId, Guid? facilityId, string? search, CancellationToken cancellationToken);
    Task<DeviceGroupResponse> GetByIdAsync(Guid id, UserScopeContext context, CancellationToken cancellationToken);
    Task<DeviceGroupResponse> CreateAsync(CreateDeviceGroupRequest request, UserScopeContext context, CancellationToken cancellationToken);
    Task<DeviceGroupResponse> UpdateAsync(Guid id, UpdateDeviceGroupRequest request, UserScopeContext context, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, UserScopeContext context, CancellationToken cancellationToken);
}

public interface IDeviceVendorService
{
    Task<IReadOnlyList<DeviceVendorResponse>> GetAllAsync(UserScopeContext context, Guid? organizationId, string? search, CancellationToken cancellationToken);
    Task<DeviceVendorResponse> GetByIdAsync(Guid id, UserScopeContext context, CancellationToken cancellationToken);
    Task<DeviceVendorResponse> CreateAsync(CreateDeviceVendorRequest request, UserScopeContext context, CancellationToken cancellationToken);
    Task<DeviceVendorResponse> UpdateAsync(Guid id, UpdateDeviceVendorRequest request, UserScopeContext context, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, UserScopeContext context, CancellationToken cancellationToken);
}

public interface IDeviceErrorCodeService
{
    Task<IReadOnlyList<DeviceErrorCodeResponse>> GetAllAsync(string? search, string? severity, CancellationToken cancellationToken);
    Task<DeviceErrorCodeResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<DeviceErrorCodeResponse> CreateAsync(CreateDeviceErrorCodeRequest request, UserScopeContext context, CancellationToken cancellationToken);
    Task<DeviceErrorCodeResponse> UpdateAsync(Guid id, UpdateDeviceErrorCodeRequest request, UserScopeContext context, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, UserScopeContext context, CancellationToken cancellationToken);
}

public interface IDeviceActivityLogService
{
    Task<IReadOnlyList<DeviceActivityLogResponse>> GetAllAsync(UserScopeContext context, Guid? deviceId, string? actionType, string? result, CancellationToken cancellationToken);
    Task<DeviceActivityLogResponse> GetByIdAsync(Guid id, UserScopeContext context, CancellationToken cancellationToken);
    Task<DeviceActivityLogResponse> CreateAsync(CreateDeviceActivityLogRequest request, CancellationToken cancellationToken);
    Task<DeviceActivityLogResponse> UpdateAsync(Guid id, UpdateDeviceActivityLogRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public interface IDevicePlayHistoryService
{
    Task<IReadOnlyList<DevicePlayHistoryResponse>> GetAllAsync(UserScopeContext context, Guid? deviceId, Guid? broadcastId, string? result, CancellationToken cancellationToken);
    Task<DevicePlayHistoryResponse> GetByIdAsync(Guid id, UserScopeContext context, CancellationToken cancellationToken);
    Task<DevicePlayHistoryResponse> CreateAsync(CreateDevicePlayHistoryRequest request, CancellationToken cancellationToken);
    Task<DevicePlayHistoryResponse> UpdateAsync(Guid id, UpdateDevicePlayHistoryRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
