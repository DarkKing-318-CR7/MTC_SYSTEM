using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MTC_System.Data;
using MTC_System.DTOs.MediaCategories;
using MTC_System.Models;
using MTC_System.Services.Interfaces;

namespace MTC_System.Services.Implements;

public sealed class MediaCategoryService : IMediaCategoryService
{
    private readonly MyDbContext _db;

    public MediaCategoryService(MyDbContext db) => _db = db;

    public async Task<IReadOnlyList<MediaCategoryResponse>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var categories = await _db.MediaCategories.AsNoTracking()
            .Where(x => !x.IsDeleted && (x.OrganizationId == null || x.OrganizationId == organizationId))
            .OrderBy(x => x.OrganizationId != null)
            .ThenBy(x => x.CategoryName)
            .ToListAsync(cancellationToken);
        return categories.Select(Map).ToList();
    }

    public async Task<MediaCategoryResponse> GetByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken)
    {
        var category = await FindCategoryAsync(id, organizationId, cancellationToken);
        return Map(category);
    }

    public async Task<MediaCategoryResponse> CreateAsync(CreateMediaCategoryRequest request, Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        var name = NormalizeCategoryName(request.CategoryName);
        await EnsureUniqueAsync(name, organizationId, null, cancellationToken);
        var category = new MediaCategory
        {
            OrganizationId = organizationId,
            CategoryName = name,
            Description = request.Description?.Trim(),
            IsDefault = false,
            CreatedBy = userId
        };
        _db.MediaCategories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(category);
    }

    public async Task<MediaCategoryResponse> UpdateAsync(Guid id, UpdateMediaCategoryRequest request, Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        var category = await FindCategoryAsync(id, organizationId, cancellationToken);
        EnsureOrganizationCategory(category);
        var name = NormalizeCategoryName(request.CategoryName);
        await EnsureUniqueAsync(name, organizationId, id, cancellationToken);
        category.CategoryName = name;
        category.Description = request.Description?.Trim();
        category.UpdatedAt = DateTimeOffset.UtcNow;
        category.UpdatedBy = userId;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(category);
    }

    private static string NormalizeCategoryName(string? categoryName)
    {
        var normalized = categoryName?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > 255)
            throw Error(StatusCodes.Status400BadRequest, "Tên lĩnh vực bắt buộc và phải dài từ 1 đến 255 ký tự sau khi chuẩn hóa.");
        return normalized;
    }

    public async Task DeleteAsync(Guid id, Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        var category = await FindCategoryAsync(id, organizationId, cancellationToken);
        EnsureOrganizationCategory(category);
        var referenced = await _db.Media.AnyAsync(x => x.CategoryId == id, cancellationToken);
        if (referenced)
            throw Error(StatusCodes.Status409Conflict, "Không thể xóa lĩnh vực đang được media sử dụng.");

        category.IsDeleted = true;
        category.UpdatedAt = DateTimeOffset.UtcNow;
        category.UpdatedBy = userId;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<MediaCategory> FindCategoryAsync(Guid id, Guid organizationId, CancellationToken cancellationToken) =>
        await _db.MediaCategories.FirstOrDefaultAsync(
            x => x.Id == id && !x.IsDeleted && (x.OrganizationId == null || x.OrganizationId == organizationId), cancellationToken)
        ?? throw Error(StatusCodes.Status404NotFound, "Không tìm thấy lĩnh vực.");

    private async Task EnsureUniqueAsync(string name, Guid organizationId, Guid? ignoredId, CancellationToken cancellationToken)
    {
        var duplicate = await _db.MediaCategories.AnyAsync(x => x.CategoryName == name && x.OrganizationId == organizationId &&
            (!ignoredId.HasValue || x.Id != ignoredId.Value), cancellationToken);
        if (duplicate)
            throw Error(StatusCodes.Status409Conflict, "Tên lĩnh vực đã tồn tại trong phạm vi khả dụng.");
    }

    private static void EnsureOrganizationCategory(MediaCategory category)
    {
        if (category.OrganizationId is null || category.IsDefault)
            throw Error(StatusCodes.Status403Forbidden, "Không được sửa hoặc xóa lĩnh vực mặc định toàn hệ thống.");
    }

    private static MediaCategoryResponse Map(MediaCategory category) => new()
    {
        Id = category.Id,
        CategoryName = category.CategoryName,
        Description = category.Description,
        IsDefault = category.IsDefault,
        OrganizationId = category.OrganizationId,
        CreatedAt = category.CreatedAt,
        UpdatedAt = category.UpdatedAt
    };

    private static MediaModuleException Error(int statusCode, string message) => new(statusCode, message);
}
