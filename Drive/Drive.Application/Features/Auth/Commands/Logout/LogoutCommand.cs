using MediatR;

namespace Drive.Application.Features.Auth.Commands.Logout;

public sealed record LogoutCommand(
    string RefreshToken) : IRequest<bool>;