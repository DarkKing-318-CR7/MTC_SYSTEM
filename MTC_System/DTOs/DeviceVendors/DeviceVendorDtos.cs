using System.ComponentModel.DataAnnotations;

namespace MTC_System.DTOs.DeviceVendors;

public class CreateDeviceVendorRequest
{
    public Guid? OrganizationId { get; set; }

    [Required, StringLength(255)]
    public string VendorName { get; set; } = string.Empty;

    [EmailAddress, StringLength(255)]
    public string? ContactEmail { get; set; }

    [StringLength(20)]
    public string? ContactPhone { get; set; }

    [Url, StringLength(255)]
    public string? Website { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    public bool IsDefault { get; set; }
}

public class UpdateDeviceVendorRequest : CreateDeviceVendorRequest;

public class DeviceVendorResponse
{
    public Guid Id { get; set; }
    public Guid? OrganizationId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Website { get; set; }
    public string? Address { get; set; }
    public bool IsDefault { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
