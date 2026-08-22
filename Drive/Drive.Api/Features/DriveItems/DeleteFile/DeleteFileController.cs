using Drive.Application.Features.DriveItems.Commands.DeleteFile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Features.DriveItems.DeleteFile;

[Authorize]
[ApiController]
[Route("api/drive-items")]
public sealed class DeleteFileController : ControllerBase
{
    private readonly ISender _sender;

    public DeleteFileController(ISender sender)
    {
        _sender = sender;
    }

    [HttpDelete("files/{driveItemId:guid}")]
    public async Task<IActionResult> DeleteFile(
        Guid driveItemId,
        CancellationToken cancellationToken)
    {
        var deleted = await _sender.Send(
            new DeleteFileCommand(driveItemId),
            cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}