using Drive.Application.Features.DriveItems.Commands.EmptyTrash;
using Drive.Application.Features.DriveItems.Commands.HardDeleteDriveItem;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Features.DriveItems.Trash;

[Authorize]
[ApiController]
[Route("api/drive-items/trash")]
public class TrashController : ControllerBase
{
    private readonly ISender _sender;

    public TrashController(ISender sender)
    {
        _sender = sender;
    }

    [HttpDelete("{driveItemId:guid}")]
    public async Task<IActionResult> HardDelete(
        Guid driveItemId,
        CancellationToken cancellationToken)
    {
        var success = await _sender.Send(
            new HardDeleteDriveItemCommand(driveItemId),
            cancellationToken);

        if (!success)
        {
            return NotFound("Item not found in trash, or you do not have permission to delete it.");
        }

        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> EmptyTrash(CancellationToken cancellationToken)
    {
        var count = await _sender.Send(new EmptyTrashCommand(), cancellationToken);

        return NoContent();
    }
}
