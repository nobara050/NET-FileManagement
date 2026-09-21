using Drive.Application.Features.DriveItems.Models;
using MediatR;

namespace Drive.Application.Features.DriveItems.Queries.GetDownloadUrl;

public sealed record GetDownloadUrlQuery(Guid DriveItemId)
    : IRequest<DownloadUrlResult>;
