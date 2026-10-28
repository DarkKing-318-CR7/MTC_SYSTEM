using Microsoft.AspNetCore.Mvc;
using MTC_System.Common;
using MTC_System.DTOs.MediaUpload;
using MTC_System.Services.Interfaces;

namespace MTC_System.Controllers;

[ApiController]
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
            var result = await _uploadService.InitializeAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetStatus), new { uploadIdentifier = result.UploadIdentifier }, result);
        }
        catch (MediaUploadException ex) { return Problem(statusCode: ex.StatusCode, title: "Unable to initialize upload", detail: ex.Message); }
    }

    [HttpPut("{uploadIdentifier}/chunks/{chunkIndex:int}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(MediaUploadStatusResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadChunk(string uploadIdentifier, int chunkIndex, [FromForm] UploadMediaChunkRequestDto request, CancellationToken cancellationToken)
    {
        try { return Ok(await _uploadService.UploadChunkAsync(uploadIdentifier, chunkIndex, request, cancellationToken)); }
        catch (MediaUploadException ex) { return Problem(statusCode: ex.StatusCode, title: "Unable to accept upload chunk", detail: ex.Message); }
    }

    [HttpGet("{uploadIdentifier}")]
    [ProducesResponseType(typeof(MediaUploadStatusResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(string uploadIdentifier, CancellationToken cancellationToken)
    {
        try { return Ok(await _uploadService.GetStatusAsync(uploadIdentifier, cancellationToken)); }
        catch (MediaUploadException ex) { return Problem(statusCode: ex.StatusCode, title: "Unable to retrieve upload status", detail: ex.Message); }
    }

    [HttpPost("{uploadIdentifier}/complete")]
    [ProducesResponseType(typeof(CompleteMediaUploadResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Complete(string uploadIdentifier, CancellationToken cancellationToken)
    {
        try { return Ok(await _uploadService.CompleteAsync(uploadIdentifier, cancellationToken)); }
        catch (MediaUploadException ex) { return Problem(statusCode: ex.StatusCode, title: "Unable to complete upload", detail: ex.Message); }
    }
}