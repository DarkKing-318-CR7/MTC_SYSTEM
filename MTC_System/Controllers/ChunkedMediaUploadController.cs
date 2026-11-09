using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTC_System.Common;
using MTC_System.DTOs.MediaUpload;
using MTC_System.Services.Interfaces;

namespace MTC_System.Controllers;

[ApiController]
[Authorize(Roles = "CREATOR,ADMINISTRATOR,Administrator")]
[Route("api/media-uploads")]
public sealed class ChunkedMediaUploadController : ControllerBase
{
    private readonly IChunkedMediaUploadSvc _uploadService;

    public ChunkedMediaUploadController(IChunkedMediaUploadSvc uploadService) => _uploadService = uploadService;

    [HttpPost]
    [ProducesResponseType(typeof(InitializeMediaUploadResponseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Initialize([FromBody] InitializeMediaUploadRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            if (!TryGetUserId(out var userId) || !await ApplyAuthenticatedScopeAsync(request, cancellationToken)) return Forbid();
            var result = await _uploadService.InitializeAsync(request, userId, cancellationToken);
            return CreatedAtAction(nameof(GetStatus), new { uploadIdentifier = result.UploadIdentifier }, result);
        }
        catch (MediaUploadException ex) { return Problem(statusCode: ex.StatusCode, title: "Unable to initialize upload", detail: ex.Message); }
    }

    [HttpPut("{uploadIdentifier}/chunks/{chunkIndex:int}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(MediaUploadStatusResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadChunk(string uploadIdentifier, int chunkIndex, [FromForm] UploadMediaChunkRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            if (!TryGetUserId(out var userId)) return Forbid();
            return Ok(await _uploadService.UploadChunkAsync(uploadIdentifier, chunkIndex, request, userId, cancellationToken));
        }
        catch (MediaUploadException ex) { return Problem(statusCode: ex.StatusCode, title: "Unable to accept upload chunk", detail: ex.Message); }
    }

    [HttpGet("{uploadIdentifier}")]
    [ProducesResponseType(typeof(MediaUploadStatusResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(string uploadIdentifier, CancellationToken cancellationToken)
    {
        try
        {
            if (!TryGetUserId(out var userId)) return Forbid();
            return Ok(await _uploadService.GetStatusAsync(uploadIdentifier, userId, cancellationToken));
        }
        catch (MediaUploadException ex) { return Problem(statusCode: ex.StatusCode, title: "Unable to retrieve upload status", detail: ex.Message); }
    }

    [HttpPost("{uploadIdentifier}/complete")]
    [ProducesResponseType(typeof(CompleteMediaUploadResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Complete(string uploadIdentifier, CancellationToken cancellationToken)
    {
        try
        {
            if (!TryGetUserId(out var userId)) return Forbid();
            return Ok(await _uploadService.CompleteAsync(uploadIdentifier, userId, cancellationToken));
        }
        catch (MediaUploadException ex) { return Problem(statusCode: ex.StatusCode, title: "Unable to complete upload", detail: ex.Message); }
    }

    private async Task<bool> ApplyAuthenticatedScopeAsync(InitializeMediaUploadRequestDto request, CancellationToken cancellationToken)
    {
        var organizationClaim = User.FindFirstValue("organizationId");
        var facilityClaim = User.FindFirstValue("facilityId");
        if (Guid.TryParse(organizationClaim, out var organizationId) && organizationId != Guid.Empty)
        {
            if (request.OrganizationId != Guid.Empty && request.OrganizationId != organizationId) return false;
            request.OrganizationId = organizationId;
            if (facilityClaim is not null)
            {
                if (!Guid.TryParse(facilityClaim, out var facilityId)) return false;
                if (request.FacilityId == Guid.Empty || !await _uploadService.IsFacilityWithinScopeAsync(request.FacilityId, organizationId, facilityId, cancellationToken)) return false;
            }
            return true;
        }

        return false;
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}