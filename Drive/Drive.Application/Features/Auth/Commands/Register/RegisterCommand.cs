using Drive.Application.Features.Auth.Models;
using MediatR;

namespace Drive.Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string DisplayName) : IRequest<RegisterResult>;