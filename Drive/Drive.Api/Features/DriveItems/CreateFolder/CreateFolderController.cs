using AutoMapper;
using Drive.Api.Features.DriveItems.List;
using Drive.Application.Features.DriveItems.Commands.CreateFolder;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Features.DriveItems.CreateFolder;

[Authorize]
[ApiController]
[Route("api/drive-items")]
public sealed class CreateFolderController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;

    public CreateFolderController(
        ISender sender,
        IMapper mapper)
    {
        _sender = sender;
        _mapper = mapper;
    }

    [HttpPost("folders")]
    public async Task<ActionResult<DriveItemResponse>> CreateFolder(
        CreateFolderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateFolderCommand(
                request.ParentId,
                request.Name),
            cancellationToken);

        if (result is null)
        {
            return BadRequest();
        }

        return Created(
            $"/api/drive-items?parentId={result.ParentId}",
            _mapper.Map<DriveItemResponse>(result));
    }
}