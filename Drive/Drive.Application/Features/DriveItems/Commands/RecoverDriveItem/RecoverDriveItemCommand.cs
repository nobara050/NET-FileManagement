using Drive.Application.Features.DriveItems.Models;
using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.RecoverDriveItem;

public sealed record RecoverDriveItemCommand(Guid DriveItemId) : IRequest<DriveItemResult>;
