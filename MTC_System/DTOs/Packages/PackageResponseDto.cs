namespace MTC_System.DTOs.Packages
{
    public class PackageResponseDto
    {
        public Guid Id { get; set; }

        public string PackageCode { get; set; } = string.Empty;

        public string PackageName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal PricePerMonth { get; set; }

        public int MaxDevices { get; set; }

        public int MaxStorageGB { get; set; }

        public int DurationMonths { get; set; }

        public bool IsActive { get; set; }

        public bool IsCustom { get; set; }
    }
}