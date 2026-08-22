using Drive.Application.Features.Auth.Commands.Logout;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Features.Auth.Logout;

[ApiController]
[Route("api/auth")]
public sealed class LogoutController : ControllerBase
{
    private readonly ISender _sender;

    public LogoutController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        LogoutRequest request,
        CancellationToken cancellationToken)
    {
        var revoked = await _sender.Send(
            new LogoutCommand(request.RefreshToken),
            cancellationToken);

        if (!revoked)
        {
            return Unauthorized();
        }

        return NoContent();
    }
}