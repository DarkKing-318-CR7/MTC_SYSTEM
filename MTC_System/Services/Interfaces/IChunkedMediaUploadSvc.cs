using MTC_System.DTOs.MediaUpload;

namespace MTC_System.Services.Interfaces;

public interface IChunkedMediaUploadSvc
{
    Task<bool> IsFacilityWithinScopeAsync(Guid facilityId, Guid organizationId, Guid rootFacilityId, CancellationToken cancellationToken);
    Task<InitializeMediaUploadResponseDto> InitializeAsync(InitializeMediaUploadRequestDto request, Guid userId, CancellationToken cancellationToken);
    Task<MediaUploadStatusResponseDto> UploadChunkAsync(string uploadIdentifier, int chunkIndex, UploadMediaChunkRequestDto request, Guid userId, CancellationToken cancellationToken);
    Task<MediaUploadStatusResponseDto> GetStatusAsync(string uploadIdentifier, Guid userId, CancellationToken cancellationToken);
    Task<CompleteMediaUploadResponseDto> CompleteAsync(string uploadIdentifier, Guid userId, CancellationToken cancellationToken);
}
