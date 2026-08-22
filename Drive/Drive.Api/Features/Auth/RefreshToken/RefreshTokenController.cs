using Drive.Application.Features.Auth.Commands.RefreshToken;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Features.Auth.RefreshToken;

[ApiController]
[Route("api/auth")]
public sealed class RefreshTokenController : ControllerBase
{
    private readonly ISender _sender;

    public RefreshTokenController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponse>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RefreshTokenCommand(request.RefreshToken),
            cancellationToken);

        if (result is null)
        {
            return Unauthorized();
        }

        return Ok(new TokenResponse
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken
        });
    }
}