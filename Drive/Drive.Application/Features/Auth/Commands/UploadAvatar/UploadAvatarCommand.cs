using Drive.Application.Features.DriveItems.Models;
using MediatR;

namespace Drive.Application.Features.Auth.Commands.UploadAvatar;

public sealed record UploadAvatarCommand(FileUpload File) : IRequest<string?>;
