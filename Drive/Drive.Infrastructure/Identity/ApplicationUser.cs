using Drive.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Drive.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; }
    = new List<RefreshToken>();
}