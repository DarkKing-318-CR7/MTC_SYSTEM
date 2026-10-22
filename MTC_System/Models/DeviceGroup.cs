using System;
using System.Text.Json.Serialization;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 4] Bảng DeviceGroups (Nhóm thiết bị)
    /// </summary>
    public class DeviceGroup : FacilityScopedEntity
    {
        public string GroupName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        [JsonIgnore]
        public virtual ICollection<Device> Devices { get; set; } = new List<Device>();
    }
}
