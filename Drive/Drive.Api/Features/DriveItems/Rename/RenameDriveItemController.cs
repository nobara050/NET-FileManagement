using AutoMapper;
using Drive.Api.Features.DriveItems.List;
using Drive.Application.Features.DriveItems.Commands.RenameDriveItem;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Features.DriveItems.Rename;

[Authorize]
[ApiController]
[Route("api/drive-items/{driveItemId:guid}/rename")]
public class RenameDriveItemController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;

    public RenameDriveItemController(ISender sender, IMapper mapper)
    {
        _sender = sender;
        _mapper = mapper;
    }

    [HttpPatch]
    public async Task<IActionResult> Rename(
        Guid driveItemId,
        [FromBody] RenameDriveItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RenameDriveItemCommand(driveItemId, request.Name),
            cancellationToken);

        var response = _mapper.Map<DriveItemResponse>(result);
        return Ok(response);
    }
}

public sealed record RenameDriveItemRequest(string Name);
