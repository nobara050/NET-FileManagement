using Drive.Application.Features.DriveItems.Models;
using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.CreateFolder;

public sealed record CreateFolderCommand(
    Guid? ParentId,
    string Name)
    : IRequest<DriveItemResult>;