using Drive.Application.Features.Users.Commands.DeleteUser;
using Drive.Application.Features.Users.Queries.ListUsers;
using Drive.Application.Features.Users.Queries.GetUser;
using Drive.Application.Features.Users.Queries.SearchUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace Drive.Api.Features.Users;

[Authorize]
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string query,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new SearchUsersQuery(query, limit),
            cancellationToken);

        return Ok(result);
    }

    [Authorize(Policy = "Admin")]
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListUsersQuery(), cancellationToken);

        return Ok(result);
    }

    [Authorize(Policy = "Admin")]
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> Get(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetUserQuery(userId), cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [Authorize(Policy = "Admin")]
    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Delete(Guid userId, CancellationToken cancellationToken)
    {
        var deleted = await _sender.Send(new DeleteUserCommand(userId), cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
