using Drive.Application.Features.DriveItems.Models;
using MediatR;

namespace Drive.Application.Features.DriveItems.Queries.GetPreviewUrl;

public sealed record GetPreviewUrlQuery(Guid DriveItemId)
    : IRequest<DownloadUrlResult>;
