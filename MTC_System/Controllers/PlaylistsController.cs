using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTC_System.Common;
using MTC_System.DTOs.Playlists;
using MTC_System.Services.Interfaces;

namespace MTC_System.Controllers;

[ApiController]
[Authorize]
[Route("api/playlists")]
public sealed class PlaylistsController : ControllerBase
{
    private readonly IPlaylistService _service;

    public PlaylistsController(IPlaylistService service) => _service = service;

    [HttpGet]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PlaylistResponse>>>> GetAll(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? facilityId,
        CancellationToken cancellationToken)
    {
        var data = await _service.GetAllAsync(User.GetUserScope(), organizationId, facilityId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PlaylistResponse>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR")]
    public async Task<ActionResult<ApiResponse<PlaylistResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.GetByIdAsync(id, User.GetUserScope(), cancellationToken);
        return Ok(ApiResponse<PlaylistResponse>.Ok(data));
    }

    [HttpPost]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR")]
    public async Task<ActionResult<ApiResponse<PlaylistResponse>>> Create(
        [FromBody] PlaylistUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var data = await _service.CreateAsync(request, User.GetUserScope(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<PlaylistResponse>.Ok(data, "Tạo Playlist thành công."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR")]
    public async Task<ActionResult<ApiResponse<PlaylistResponse>>> Update(
        Guid id,
        [FromBody] PlaylistUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var data = await _service.UpdateAsync(id, request, User.GetUserScope(), cancellationToken);
        return Ok(ApiResponse<PlaylistResponse>.Ok(data, "Cập nhật Playlist thành công."));
    }

    [HttpGet("{id:guid}/items")]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PlaylistItemResponse>>>> GetItems(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.GetItemsAsync(id, User.GetUserScope(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PlaylistItemResponse>>.Ok(data));
    }

    [HttpPut("{id:guid}/items")]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PlaylistItemResponse>>>> ReplaceItems(
        Guid id,
        [FromBody] IReadOnlyList<PlaylistItemUpsertRequest> items,
        CancellationToken cancellationToken)
    {
        var data = await _service.ReplaceItemsAsync(id, items, User.GetUserScope(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PlaylistItemResponse>>.Ok(data, "Cập nhật thứ tự Playlist thành công."));
    }

    [HttpPost("{id:guid}/items")]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR")]
    public async Task<ActionResult<ApiResponse<PlaylistItemResponse>>> AddItem(
        Guid id,
        [FromBody] PlaylistItemUpsertRequest item,
        CancellationToken cancellationToken)
    {
        var data = await _service.AddItemAsync(id, item, User.GetUserScope(), cancellationToken);
        return Ok(ApiResponse<PlaylistItemResponse>.Ok(data, "Thêm Media vào Playlist thành công."));
    }

    [HttpDelete("{id:guid}/items/{mediaId:guid}")]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveItem(Guid id, Guid mediaId, CancellationToken cancellationToken)
    {
        await _service.RemoveItemAsync(id, mediaId, User.GetUserScope(), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { id, mediaId }, "Xóa Media khỏi Playlist thành công."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, User.GetUserScope(), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { id }, "Xóa Playlist thành công."));
    }
}
