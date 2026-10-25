using System;

namespace MTC_System.DTOs
{
    public class CreateOrganizationDto
    {
        public string OrganizationCode { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTimeOffset EffectiveDate { get; set; } = DateTimeOffset.UtcNow;
    }

    public class UpdateOrganizationDto
    {
        public string OrganizationName { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Status { get; set; } = "Còn hạn"; // Còn hạn, Hết hạn, Hủy bỏ
    }

    public class OrganizationResponseDto
    {
        public Guid Id { get; set; }
        public string OrganizationCode { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset EffectiveDate { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
