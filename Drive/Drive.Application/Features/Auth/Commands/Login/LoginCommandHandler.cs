using Drive.Application.Common.Interfaces;
using Drive.Application.Features.Auth.Models;
using MediatR;

namespace Drive.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler
    : IRequestHandler<LoginCommand, AuthTokenResult?>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;

    public LoginCommandHandler(
        IIdentityService identityService,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService)
    {
        _identityService = identityService;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<AuthTokenResult?> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var userId = await _identityService.AuthenticateAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (userId is null)
        {
            return null;
        }

        var accessToken = _jwtTokenService.GenerateToken(
            userId.Value);

        var refreshToken = await _refreshTokenService.CreateAsync(
            userId.Value,
            cancellationToken);

        return new AuthTokenResult(
            accessToken,
            refreshToken);
    }
}