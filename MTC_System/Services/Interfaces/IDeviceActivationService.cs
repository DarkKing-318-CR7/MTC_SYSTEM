using MTC_System.DTOs.Devices;

namespace MTC_System.Services.Interfaces;

public interface IDeviceActivationService
{
    Task<DeviceActivationCodeResponse> GenerateActivationCodeAsync(
        Guid deviceId,
        DeviceActivationAdminContext admin,
        CancellationToken cancellationToken);

    Task<DeviceTokenResponse> ActivateAsync(
        ActivateDeviceRequest request,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task<DeviceTokenResponse> RefreshAsync(
        RefreshDeviceTokenRequest request,
        string? ipAddress,
        CancellationToken cancellationToken);
}
