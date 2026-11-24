using MTC_System.DTOs.Media;

namespace MTC_System.Services.Interfaces;

public interface IMediaService
{
    Task<bool> CanAccessFacilityAsync(Guid organizationId, Guid? rootFacilityId, Guid targetFacilityId, CancellationToken cancellationToken);
    Task<IReadOnlyList<MediaResponse>> GetAllAsync(Guid organizationId, Guid? facilityId, CancellationToken cancellationToken);
    Task<MediaResponse> GetByIdAsync(Guid id, Guid organizationId, Guid? facilityId, CancellationToken cancellationToken);
    Task<MediaResponse> CreateAsync(CreateMediaRequest request, Guid organizationId, Guid facilityId, Guid userId, CancellationToken cancellationToken);
    Task<MediaResponse> UpdateAsync(Guid id, UpdateMediaRequest request, Guid organizationId, Guid? facilityId, Guid userId, CancellationToken cancellationToken);
    Task<MediaResponse> ApproveAsync(Guid id, Guid organizationId, Guid? facilityId, Guid checkerId, CancellationToken cancellationToken);
    Task<MediaResponse> RejectAsync(Guid id, string? reason, Guid organizationId, Guid? facilityId, Guid checkerId, CancellationToken cancellationToken);
}
