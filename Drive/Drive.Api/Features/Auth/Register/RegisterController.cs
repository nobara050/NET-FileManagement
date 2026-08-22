using Drive.Application.Features.Auth.Commands.Register;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Features.Auth.Register;

[ApiController]
[Route("api/auth")]
public sealed class RegisterController : ControllerBase
{
    private readonly ISender _sender;

    public RegisterController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RegisterCommand(
                request.Email,
                request.Password,
                request.DisplayName),
            cancellationToken);

        if (result.IsEmailConflict)
        {
            return Conflict(result.Errors);
        }

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new RegisterResponse
        {
            UserId = result.UserId
        });
    }
}