using System.ComponentModel.DataAnnotations;

namespace MTC_System.DTOs.Templates;

public sealed class TemplateUpsertRequest
{
    public Guid? OrganizationId { get; set; }
    [Required] public Guid FacilityId { get; set; }
    [Required, StringLength(255)] public string TemplateName { get; set; } = string.Empty;
    [StringLength(1000)] public string? Description { get; set; }
    [Range(1, int.MaxValue)] public int Width { get; set; } = 1920;
    [Range(1, int.MaxValue)] public int Height { get; set; } = 1080;
    public string? LayoutConfigJson { get; set; }
    public bool IsActive { get; set; } = true;
    public List<TemplateZoneUpsertRequest> Zones { get; set; } = [];
}

public sealed class TemplateZoneUpsertRequest
{
    public Guid? Id { get; set; }
    [Required, StringLength(100)] public string ZoneName { get; set; } = string.Empty;
    [Range(0, int.MaxValue)] public int X { get; set; }
    [Range(0, int.MaxValue)] public int Y { get; set; }
    [Range(typeof(decimal), "0.01", "100")] public decimal WidthPercent { get; set; } = 100;
    [Range(typeof(decimal), "0.01", "100")] public decimal HeightPercent { get; set; } = 100;
    [Range(0, int.MaxValue)] public int ZIndex { get; set; }
    [StringLength(200)] public string AllowedMediaTypes { get; set; } = "Video,Audio,Image,URL";
}

public sealed class TemplateResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid FacilityId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string? LayoutConfigJson { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public IReadOnlyList<TemplateZoneResponse> Zones { get; set; } = [];
}

public sealed class TemplateZoneResponse
{
    public Guid Id { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public decimal WidthPercent { get; set; }
    public decimal HeightPercent { get; set; }
    public int ZIndex { get; set; }
    public string AllowedMediaTypes { get; set; } = string.Empty;
}
