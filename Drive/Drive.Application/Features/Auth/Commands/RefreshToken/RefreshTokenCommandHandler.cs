using Drive.Application.Common.Interfaces;
using Drive.Application.Features.Auth.Models;
using MediatR;

namespace Drive.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, AuthTokenResult?>
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;

    public RefreshTokenCommandHandler(
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService)
    {
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<AuthTokenResult?> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _refreshTokenService.RotateAsync(
            request.RefreshToken,
            cancellationToken);

        if (result is null)
        {
            return null;
        }

        var accessToken = _jwtTokenService.GenerateToken(
            result.UserId);

        return new AuthTokenResult(
            accessToken,
            result.RefreshToken);
    }
}