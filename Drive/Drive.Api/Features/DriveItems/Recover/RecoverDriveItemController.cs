using AutoMapper;
using Drive.Api.Features.DriveItems.List;
using Drive.Application.Features.DriveItems.Commands.RecoverDriveItem;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Features.DriveItems.Recover;

[Authorize]
[ApiController]
[Route("api/drive-items")]
public class RecoverDriveItemController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;

    public RecoverDriveItemController(ISender sender, IMapper mapper)
    {
        _sender = sender;
        _mapper = mapper;
    }

    [HttpPost("{driveItemId:guid}/recover")]
    public async Task<ActionResult<DriveItemResponse>> Recover(Guid driveItemId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RecoverDriveItemCommand(driveItemId), cancellationToken);

        return Ok(_mapper.Map<DriveItemResponse>(result));
    }
}
