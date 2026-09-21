using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.RemoveAssignment;

public sealed record RemoveAssignmentCommand(Guid DriveItemId, Guid TargetUserId) : IRequest<bool>;
