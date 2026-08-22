using Drive.Application.Features.Auth.Models;
using MediatR;

namespace Drive.Application.Features.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(
    string RefreshToken) : IRequest<AuthTokenResult?>;