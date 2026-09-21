using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.HardDeleteDriveItem;

public sealed record HardDeleteDriveItemCommand(Guid DriveItemId) : IRequest<bool>;
