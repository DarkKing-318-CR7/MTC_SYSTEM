using System;

namespace MTC_System.DTOs.Subscriptions
{
    public class SubscriptionResponseDto
    {
        public Guid Id { get; set; }

        public Guid OrganizationId { get; set; }

        public Guid? PackageId { get; set; }

        public DateTimeOffset StartDate { get; set; }

        public DateTimeOffset EndDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public int? CustomStorageGB { get; set; }

        public int? CustomMaxDevices { get; set; }

        public int? CustomDurationMonths { get; set; }

        public decimal? CustomPrice { get; set; }

        public string? CustomPackageName { get; set; }

        public decimal ActualPrice { get; set; }

        public string? Notes { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}