using MTC_System.DTOs.DeviceActivityLogs;
using MTC_System.DTOs.DeviceErrorCodes;
using MTC_System.DTOs.DeviceGroups;
using MTC_System.DTOs.DevicePlayHistories;
using MTC_System.DTOs.Devices;
using MTC_System.DTOs.DeviceVendors;

namespace MTC_System.Services.Interfaces;

public interface IDeviceService
{
    Task<IReadOnlyList<DeviceResponse>> GetAllAsync(Guid? organizationId, Guid? facilityId, string? search, CancellationToken cancellationToken);
    Task<DeviceResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<DeviceResponse> CreateAsync(CreateDeviceRequest request, CancellationToken cancellationToken);
    Task<DeviceResponse> UpdateAsync(Guid id, UpdateDeviceRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public interface IDeviceGroupService
{
    Task<IReadOnlyList<DeviceGroupResponse>> GetAllAsync(Guid? organizationId, Guid? facilityId, string? search, CancellationToken cancellationToken);
    Task<DeviceGroupResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<DeviceGroupResponse> CreateAsync(CreateDeviceGroupRequest request, CancellationToken cancellationToken);
    Task<DeviceGroupResponse> UpdateAsync(Guid id, UpdateDeviceGroupRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public interface IDeviceVendorService
{
    Task<IReadOnlyList<DeviceVendorResponse>> GetAllAsync(Guid? organizationId, string? search, CancellationToken cancellationToken);
    Task<DeviceVendorResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<DeviceVendorResponse> CreateAsync(CreateDeviceVendorRequest request, CancellationToken cancellationToken);
    Task<DeviceVendorResponse> UpdateAsync(Guid id, UpdateDeviceVendorRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public interface IDeviceErrorCodeService
{
    Task<IReadOnlyList<DeviceErrorCodeResponse>> GetAllAsync(string? search, string? severity, CancellationToken cancellationToken);
    Task<DeviceErrorCodeResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<DeviceErrorCodeResponse> CreateAsync(CreateDeviceErrorCodeRequest request, CancellationToken cancellationToken);
    Task<DeviceErrorCodeResponse> UpdateAsync(Guid id, UpdateDeviceErrorCodeRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public interface IDeviceActivityLogService
{
    Task<IReadOnlyList<DeviceActivityLogResponse>> GetAllAsync(Guid? deviceId, string? actionType, string? result, CancellationToken cancellationToken);
    Task<DeviceActivityLogResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<DeviceActivityLogResponse> CreateAsync(CreateDeviceActivityLogRequest request, CancellationToken cancellationToken);
    Task<DeviceActivityLogResponse> UpdateAsync(Guid id, UpdateDeviceActivityLogRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public interface IDevicePlayHistoryService
{
    Task<IReadOnlyList<DevicePlayHistoryResponse>> GetAllAsync(Guid? deviceId, Guid? broadcastId, string? result, CancellationToken cancellationToken);
    Task<DevicePlayHistoryResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<DevicePlayHistoryResponse> CreateAsync(CreateDevicePlayHistoryRequest request, CancellationToken cancellationToken);
    Task<DevicePlayHistoryResponse> UpdateAsync(Guid id, UpdateDevicePlayHistoryRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
