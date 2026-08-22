using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.DeleteFile;

public sealed record DeleteFileCommand(
    Guid DriveItemId)
    : IRequest<bool>;