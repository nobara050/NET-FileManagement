using Drive.Application.Common.Interfaces;
using MediatR;

namespace Drive.Application.Features.Auth.Commands.Logout;

public sealed class LogoutCommandHandler
    : IRequestHandler<LogoutCommand, bool>
{
    private readonly IRefreshTokenService _refreshTokenService;

    public LogoutCommandHandler(
        IRefreshTokenService refreshTokenService)
    {
        _refreshTokenService = refreshTokenService;
    }

    public async Task<bool> Handle(
        LogoutCommand request,
        CancellationToken cancellationToken)
    {
        return await _refreshTokenService.RevokeAsync(
            request.RefreshToken,
            cancellationToken);
    }
}