using System.ComponentModel.DataAnnotations;

namespace MTC_System.DTOs.Devices;

public class CreateDeviceRequest
{
    public Guid OrganizationId { get; set; }
    public Guid FacilityId { get; set; }

    [Required, StringLength(50)]
    public string DeviceCode { get; set; } = string.Empty;

    [Required, StringLength(255)]
    public string DeviceName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? SerialNumber { get; set; }

    [StringLength(50)]
    public string? IpAddress { get; set; }

    [StringLength(20)]
    public string? SimPhoneNumber { get; set; }

    [StringLength(50)]
    public string? SimSerialNumber { get; set; }

    [StringLength(100)]
    public string? Tag { get; set; }

    [Required, StringLength(50)]
    public string OsType { get; set; } = "Android";

    [StringLength(100)]
    public string? VendorName { get; set; }

    public Guid? DeviceGroupId { get; set; }
    public bool ActiveStatus { get; set; } = true;

    [Range(0, 100)]
    public int Volume { get; set; } = 50;

    [StringLength(500)]
    public string? ScreenshotUrl { get; set; }

    [Range(0, long.MaxValue)]
    public long ScreenshotSizeInBytes { get; set; }

    public DateTimeOffset? LastScreenshotAt { get; set; }

    [Range(-90, 90)]
    public double Latitude { get; set; } = 10.7698780;

    [Range(-180, 180)]
    public double Longitude { get; set; } = 106.6511720;

    [StringLength(500)]
    public string? InstallationAddress { get; set; }
}

public class UpdateDeviceRequest : CreateDeviceRequest
{
    [Required, StringLength(50)]
    public string ConnectionStatus { get; set; } = "Mất kết nối";

    [Required, StringLength(50)]
    public string PlayStatus { get; set; } = "Dừng phát bản tin";

    public bool IsActivated { get; set; }
    public DateTimeOffset? LastOnlineAt { get; set; }
}

public class DeviceResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid FacilityId { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? IpAddress { get; set; }
    public string? SimPhoneNumber { get; set; }
    public string? SimSerialNumber { get; set; }
    public string? Tag { get; set; }
    public string OsType { get; set; } = string.Empty;
    public string? VendorName { get; set; }
    public Guid? DeviceGroupId { get; set; }
    public string? DeviceGroupName { get; set; }
    public bool ActiveStatus { get; set; }
    public string ConnectionStatus { get; set; } = string.Empty;
    public string PlayStatus { get; set; } = string.Empty;
    public int Volume { get; set; }
    public string? ScreenshotUrl { get; set; }
    public long ScreenshotSizeInBytes { get; set; }
    public DateTimeOffset? LastScreenshotAt { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? InstallationAddress { get; set; }
    public bool IsActivated { get; set; }
    public bool HasPendingActivation { get; set; }
    public DateTimeOffset? ActivationTokenExpiry { get; set; }
    public DateTimeOffset? LastOnlineAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
