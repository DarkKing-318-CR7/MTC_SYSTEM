using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 1 & Dev 3] Bảng SystemSettings (Tham số cấu hình theo từng Tổ chức)
    /// Kế thừa OrganizationScopedEntity (có OrganizationId non-null)
    /// </summary>
    public class SystemSetting : OrganizationScopedEntity
    {
        public string SettingKey { get; set; } = string.Empty;
        public string SettingValue { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
