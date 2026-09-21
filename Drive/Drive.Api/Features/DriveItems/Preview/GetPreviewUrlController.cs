using AutoMapper;
using Drive.Application.Features.DriveItems.Queries.GetPreviewUrl;
using Drive.Api.Features.DriveItems.Download;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Features.DriveItems.Preview;

[Authorize]
[ApiController]
[Route("api/drive-items")]
public sealed class GetPreviewUrlController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;

    public GetPreviewUrlController(
        ISender sender,
        IMapper mapper)
    {
        _sender = sender;
        _mapper = mapper;
    }

    /// <summary>
    /// Generates a short-lived (15 min) pre-signed S3 URL for inline preview.
    /// The URL serves the object with Content-Type set but no Content-Disposition,
    /// so browsers can render images, PDFs, and text directly.
    /// </summary>
    [HttpGet("{id:guid}/preview")]
    public async Task<ActionResult<DownloadUrlResponse>> GetPreviewUrl(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetPreviewUrlQuery(id),
            cancellationToken);

        return Ok(_mapper.Map<DownloadUrlResponse>(result));
    }
}
