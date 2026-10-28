using System.ComponentModel.DataAnnotations;

namespace MTC_System.DTOs.Packages
{
    public class PackageCreateDto
    {
        [Required]
        [StringLength(50)]
        public string PackageCode { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string PackageName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PricePerMonth { get; set; }

        [Range(0, int.MaxValue)]
        public int MaxDevices { get; set; }

        [Range(0, int.MaxValue)]
        public int MaxStorageGB { get; set; }

        [Range(1, 120)]
        public int DurationMonths { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        public bool IsCustom { get; set; } = false;
    }
}