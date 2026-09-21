using AutoMapper;
using Drive.Application.Features.DriveItems.Queries.GetDownloadUrl;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Features.DriveItems.Download;

[Authorize]
[ApiController]
[Route("api/drive-items")]
public sealed class GetDownloadUrlController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;

    public GetDownloadUrlController(
        ISender sender,
        IMapper mapper)
    {
        _sender = sender;
        _mapper = mapper;
    }

    /// <summary>
    /// Generates a pre-signed S3 download URL for the specified file.
    /// The URL is valid for 1 hour. The client should use it to download
    /// the file directly from S3 without proxying through the API.
    /// </summary>
    [HttpGet("{id:guid}/download")]
    public async Task<ActionResult<DownloadUrlResponse>> GetDownloadUrl(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetDownloadUrlQuery(id),
            cancellationToken);

        return Ok(_mapper.Map<DownloadUrlResponse>(result));
    }
}
