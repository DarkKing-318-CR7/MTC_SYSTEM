using System.ComponentModel.DataAnnotations;

namespace MTC_System.DTOs.DeviceGroups;

public class CreateDeviceGroupRequest
{
    public Guid OrganizationId { get; set; }
    public Guid FacilityId { get; set; }

    [Required, StringLength(255)]
    public string GroupName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateDeviceGroupRequest : CreateDeviceGroupRequest;

public class DeviceGroupResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid FacilityId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DeviceCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
