using Drive.Application.Features.DriveItems.Commands.RemoveAssignment;
using Drive.Application.Features.DriveItems.Commands.UpdateAssignment;
using Drive.Application.Features.DriveItems.Queries.ListAssignments;
using Drive.Application.Features.DriveItems.Commands.AssignRole;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace Drive.Api.Features.DriveItems.Assignments;

[Authorize]
[ApiController]
[Route("api/drive-items/{itemId:guid}/assignments")]
public class AssignmentsController : ControllerBase
{
    private readonly ISender _sender;

    public AssignmentsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid itemId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListAssignmentsQuery(itemId), cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Assign(Guid itemId, [FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
    {
        var success = await _sender.Send(
            new AssignRoleCommand(itemId, request.TargetUserId, request.RoleId, request.TargetEmail),
            cancellationToken);

        if (!success)
        {
            return BadRequest(
                "Could not assign role. The item may not exist, " +
                "you may not be the owner, the role may not exist, " +
                "the target user was not found, or an assignment already exists for this user.");
        }

        return NoContent();
    }

    [HttpPut("{targetUserId:guid}")]
    public async Task<IActionResult> Update(Guid itemId, Guid targetUserId, [FromBody] UpdateAssignmentRequest request, CancellationToken cancellationToken)
    {
        var success = await _sender.Send(new UpdateAssignmentCommand(itemId, targetUserId, request.NewRoleId), cancellationToken);

        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{targetUserId:guid}")]
    public async Task<IActionResult> Remove(Guid itemId, Guid targetUserId, CancellationToken cancellationToken)
    {
        var success = await _sender.Send(new RemoveAssignmentCommand(itemId, targetUserId), cancellationToken);

        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }
}

public sealed record AssignRoleRequest(Guid? TargetUserId, Guid RoleId, string? TargetEmail = null);
public sealed record UpdateAssignmentRequest(Guid NewRoleId);
