using Drive.Application.Features.Auth.Commands.UploadAvatar;
using Drive.Application.Features.Auth.Queries.GetCurrentUser;
using Drive.Application.Features.DriveItems.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Features.Auth.CurrentUser;

[Authorize]
[ApiController]
[Route("api/auth/me")]
public class CurrentUserController : ControllerBase
{
    private readonly ISender _sender;

    public CurrentUserController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [HttpGet("/api/users/me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCurrentUserQuery(), cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost("avatar")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAvatar(
        [FromForm] UploadAvatarRequest request,
        CancellationToken cancellationToken)
    {
        var file = request?.File;
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "An image file must be provided." });
        }

        using var stream = file.OpenReadStream();

        var fileUpload = new FileUpload
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length,
            Content = stream
        };

        var avatarUrl = await _sender.Send(
            new UploadAvatarCommand(fileUpload),
            cancellationToken);

        if (avatarUrl is null)
        {
            return BadRequest(new { message = "Failed to upload avatar." });
        }

        return Ok(new { avatarUrl });
    }
}
