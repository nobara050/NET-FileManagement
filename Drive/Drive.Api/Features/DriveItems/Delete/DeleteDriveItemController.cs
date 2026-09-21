using Drive.Application.Features.DriveItems.Commands.DeleteDriveItem;
using Drive.Application.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Drive.Api.Authorization;
using MediatR;

namespace Drive.Api.Features.DriveItems.Delete;

[Authorize]
[ApiController]
[Route("api/drive-items")]
public class DeleteDriveItemController : ControllerBase
{
    private readonly ISender _sender;

    public DeleteDriveItemController(ISender sender)
    {
        _sender = sender;
    }

    [RequirePermission(Permissions.DriveDelete)]
    [HttpDelete("{driveItemId:guid}")]
    public async Task<IActionResult> Delete(Guid driveItemId, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteDriveItemCommand(driveItemId), cancellationToken);

        return NoContent();
    }
}
