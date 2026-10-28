using System;
using System.ComponentModel.DataAnnotations;

namespace MTC_System.DTOs.Subscriptions
{
    public class SubscriptionCreateDto
    {
        [Required]
        public Guid OrganizationId { get; set; }

        public Guid? PackageId { get; set; }

        [Required]
        public DateTimeOffset StartDate { get; set; }

        [Required]
        public DateTimeOffset EndDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Đang hoạt động";

        [Range(0, int.MaxValue)]
        public int? CustomStorageGB { get; set; }

        [Range(0, int.MaxValue)]
        public int? CustomMaxDevices { get; set; }

        [Range(1, 120)]
        public int? CustomDurationMonths { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? CustomPrice { get; set; }

        [StringLength(255)]
        public string? CustomPackageName { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ActualPrice { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}