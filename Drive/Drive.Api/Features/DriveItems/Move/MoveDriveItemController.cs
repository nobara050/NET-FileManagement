using AutoMapper;
using Drive.Api.Features.DriveItems.List;
using Drive.Application.Features.DriveItems.Commands.MoveDriveItem;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Features.DriveItems.Move;

[Authorize]
[ApiController]
[Route("api/drive-items/{driveItemId:guid}/move")]
public class MoveDriveItemController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;

    public MoveDriveItemController(ISender sender, IMapper mapper)
    {
        _sender = sender;
        _mapper = mapper;
    }

    [HttpPatch]
    public async Task<IActionResult> Move(
        Guid driveItemId,
        [FromBody] MoveDriveItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new MoveDriveItemCommand(driveItemId, request.TargetParentId),
            cancellationToken);

        var response = _mapper.Map<DriveItemResponse>(result);
        return Ok(response);
    }
}

public sealed record MoveDriveItemRequest(Guid? TargetParentId);
