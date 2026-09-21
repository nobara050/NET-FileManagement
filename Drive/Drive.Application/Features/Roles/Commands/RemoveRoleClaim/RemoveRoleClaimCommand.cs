using MediatR;

namespace Drive.Application.Features.Roles.Commands.RemoveRoleClaim;

public sealed record RemoveRoleClaimCommand(
    Guid RoleId,
    string ClaimValue) : IRequest<bool>;
