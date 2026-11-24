using MTC_System.DTOs.MediaCategories;

namespace MTC_System.Services.Interfaces;

public interface IMediaCategoryService
{
    Task<IReadOnlyList<MediaCategoryResponse>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<MediaCategoryResponse> GetByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken);
    Task<MediaCategoryResponse> CreateAsync(CreateMediaCategoryRequest request, Guid organizationId, Guid userId, CancellationToken cancellationToken);
    Task<MediaCategoryResponse> UpdateAsync(Guid id, UpdateMediaCategoryRequest request, Guid organizationId, Guid userId, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, Guid organizationId, Guid userId, CancellationToken cancellationToken);
}
