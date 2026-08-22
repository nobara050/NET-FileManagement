using Drive.Application.Features.Auth.Commands.Login;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Features.Auth.Login;

[ApiController]
[Route("api/auth")]
public sealed class LoginController : ControllerBase
{
    private readonly ISender _sender;

    public LoginController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new LoginCommand(
                request.Email,
                request.Password),
            cancellationToken);

        if (result is null)
        {
            return Unauthorized();
        }

        return Ok(new LoginResponse
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken
        });
    }
}