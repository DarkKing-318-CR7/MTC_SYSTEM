using MTC_System.Common;
using MTC_System.DTOs.Playlists;

namespace MTC_System.Services.Interfaces;

public interface IPlaylistService
{
    Task<IReadOnlyList<PlaylistResponse>> GetAllAsync(UserScopeContext userScope, Guid? organizationId, Guid? facilityId, CancellationToken cancellationToken);
    Task<PlaylistResponse> GetByIdAsync(Guid id, UserScopeContext userScope, CancellationToken cancellationToken);
    Task<PlaylistResponse> CreateAsync(PlaylistUpsertRequest request, UserScopeContext userScope, CancellationToken cancellationToken);
    Task<PlaylistResponse> UpdateAsync(Guid id, PlaylistUpsertRequest request, UserScopeContext userScope, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlaylistItemResponse>> GetItemsAsync(Guid id, UserScopeContext userScope, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlaylistItemResponse>> ReplaceItemsAsync(Guid id, IReadOnlyList<PlaylistItemUpsertRequest> items, UserScopeContext userScope, CancellationToken cancellationToken);
    Task<PlaylistItemResponse> AddItemAsync(Guid id, PlaylistItemUpsertRequest item, UserScopeContext userScope, CancellationToken cancellationToken);
    Task RemoveItemAsync(Guid id, Guid mediaId, UserScopeContext userScope, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, UserScopeContext userScope, CancellationToken cancellationToken);
}
