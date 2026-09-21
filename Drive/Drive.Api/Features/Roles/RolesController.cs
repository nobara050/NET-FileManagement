using Drive.Application.Features.Roles.Commands.RemoveRoleClaim;
using Drive.Application.Features.Roles.Commands.AddRoleClaim;
using Drive.Application.Features.Roles.Commands.CreateRole;
using Drive.Application.Features.Roles.Commands.DeleteRole;
using Drive.Application.Features.Roles.Commands.RenameRole;
using Drive.Application.Features.Roles.Queries.ListRoles;
using Drive.Application.Features.Roles.Queries.GetRole;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace Drive.Api.Features.Roles;

[ApiController]
[Route("api/roles")]
public class RolesController : ControllerBase
{
    private readonly ISender _sender;

    public RolesController(ISender sender)
    {
        _sender = sender;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListRolesQuery(), cancellationToken);

        return Ok(result);
    }

    [Authorize(Policy = "Admin")]
    [HttpGet("{roleId:guid}")]
    public async Task<IActionResult> Get(Guid roleId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetRoleQuery(roleId), cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [Authorize(Policy = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateRoleCommand(request.Name), cancellationToken);

        if (result is null)
        {
            return Conflict("A role with that name already exists.");
        }

        return CreatedAtAction(nameof(Get), new { roleId = result.RoleId }, result);
    }

    [Authorize(Policy = "Admin")]
    [HttpPut("{roleId:guid}/name")]
    public async Task<IActionResult> Rename(Guid roleId, [FromBody] RenameRoleRequest request, CancellationToken cancellationToken)
    {
        var success = await _sender.Send(new RenameRoleCommand(roleId, request.NewName), cancellationToken);

        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    [Authorize(Policy = "Admin")]
    [HttpDelete("{roleId:guid}")]
    public async Task<IActionResult> Delete(Guid roleId, CancellationToken cancellationToken)
    {
        var success = await _sender.Send(new DeleteRoleCommand(roleId), cancellationToken);

        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    [Authorize(Policy = "Admin")]
    [HttpPost("{roleId:guid}/claims")]
    public async Task<IActionResult> AddClaim(Guid roleId, [FromBody] RoleClaimRequest request, CancellationToken cancellationToken)
    {
        var success = await _sender.Send(new AddRoleClaimCommand(roleId, request.ClaimValue), cancellationToken);

        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    [Authorize(Policy = "Admin")]
    [HttpDelete("{roleId:guid}/claims/{claimValue}")]
    public async Task<IActionResult> RemoveClaim(Guid roleId, string claimValue, CancellationToken cancellationToken)
    {
        var success = await _sender.Send(new RemoveRoleClaimCommand(roleId, claimValue), cancellationToken);

        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }
}

public sealed record CreateRoleRequest(string Name);
public sealed record RenameRoleRequest(string NewName);
public sealed record RoleClaimRequest(string ClaimValue);
