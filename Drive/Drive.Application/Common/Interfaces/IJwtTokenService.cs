namespace Drive.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(Guid userId);
}
