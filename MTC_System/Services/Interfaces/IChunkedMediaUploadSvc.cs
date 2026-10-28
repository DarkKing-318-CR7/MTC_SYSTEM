using MTC_System.DTOs.MediaUpload;

namespace MTC_System.Services.Interfaces;

public interface IChunkedMediaUploadSvc
{
    Task<InitializeMediaUploadResponseDto> InitializeAsync(InitializeMediaUploadRequestDto request, CancellationToken cancellationToken);
    Task<MediaUploadStatusResponseDto> UploadChunkAsync(string uploadIdentifier, int chunkIndex, UploadMediaChunkRequestDto request, CancellationToken cancellationToken);
    Task<MediaUploadStatusResponseDto> GetStatusAsync(string uploadIdentifier, CancellationToken cancellationToken);
    Task<CompleteMediaUploadResponseDto> CompleteAsync(string uploadIdentifier, CancellationToken cancellationToken);
}
