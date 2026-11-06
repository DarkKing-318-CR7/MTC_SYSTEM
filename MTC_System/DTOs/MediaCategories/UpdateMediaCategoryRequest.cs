using System.ComponentModel.DataAnnotations;

namespace MTC_System.DTOs.MediaCategories;

public sealed class UpdateMediaCategoryRequest
{
    private string _categoryName = string.Empty;

    [Required, StringLength(255, MinimumLength = 1)]
    public string CategoryName
    {
        get => _categoryName;
        set => _categoryName = value?.Trim() ?? string.Empty;
    }

    [StringLength(500)]
    public string? Description { get; set; }
}
