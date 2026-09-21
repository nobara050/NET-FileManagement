using Drive.Application.Features.DriveItems.Models;
using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.RenameDriveItem;

public sealed record RenameDriveItemCommand(Guid DriveItemId, string Name) : IRequest<DriveItemResult>;
