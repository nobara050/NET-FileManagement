using Drive.Application.Features.DriveItems.Models;
using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.MoveDriveItem;

public sealed record MoveDriveItemCommand(Guid DriveItemId, Guid? TargetParentId) : IRequest<DriveItemResult>;
