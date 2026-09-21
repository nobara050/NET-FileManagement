using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.AssignRole;

public record AssignRoleCommand(
    Guid DriveItemId,
    Guid? TargetUserId,
    Guid RoleId,
    string? TargetEmail = null) : IRequest<bool>;
