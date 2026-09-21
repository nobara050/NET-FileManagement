using AutoMapper;
using Drive.Api.Features.DriveItems.List;
using Drive.Application.Features.DriveItems.Commands.CreateFile;
using Drive.Application.Features.DriveItems.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Features.DriveItems.CreateFile;

[Authorize]
[ApiController]
[Route("api/drive-items")]
public sealed class CreateFileController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;

    public CreateFileController(
        ISender sender,
        IMapper mapper)
    {
        _sender = sender;
        _mapper = mapper;
    }

    [HttpPost("files")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<ActionResult<DriveItemResponse>> CreateFile(
        [FromForm] CreateFileRequest request,
        CancellationToken cancellationToken)
    {
        var file = request.File;
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = "A valid, non-empty file is required.",
                Instance = HttpContext.Request.Path
            });
        }

        var fileUpload = new FileUpload
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length,
            Content = file.OpenReadStream()
        };

        var result = await _sender.Send(
            new CreateFileCommand(
                request.ParentId,
                fileUpload),
            cancellationToken);

        return Created(
            $"/api/drive-items?parentId={result.ParentId}",
            _mapper.Map<DriveItemResponse>(result));
    }
}