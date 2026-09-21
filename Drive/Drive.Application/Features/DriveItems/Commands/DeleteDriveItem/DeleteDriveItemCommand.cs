using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.DeleteDriveItem;

public sealed record DeleteDriveItemCommand(Guid DriveItemId) : IRequest<bool>;
