using MediatR;

namespace Drive.Application.Features.Roles.Commands.AddRoleClaim;

public sealed record AddRoleClaimCommand(
    Guid RoleId,
    string ClaimValue) : IRequest<bool>;
