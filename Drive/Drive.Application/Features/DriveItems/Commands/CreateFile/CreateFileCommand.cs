using Drive.Application.Features.DriveItems.Models;
using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.CreateFile;

public sealed record CreateFileCommand(
    Guid? ParentId,
    FileUpload File)
    : IRequest<DriveItemResult?>;