using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.UpdateAssignment;

public record UpdateAssignmentCommand(Guid DriveItemId, Guid TargetUserId, Guid NewRoleId) : IRequest<bool>;
