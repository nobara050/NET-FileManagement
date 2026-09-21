using Microsoft.AspNetCore.Http;

namespace Drive.Api.Features.Auth.CurrentUser;

public class UploadAvatarRequest
{
    public IFormFile File { get; set; } = null!;
}
