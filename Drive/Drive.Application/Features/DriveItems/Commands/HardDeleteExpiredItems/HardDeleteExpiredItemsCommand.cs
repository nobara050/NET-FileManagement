using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.HardDeleteExpiredItems;

public sealed record HardDeleteExpiredItemsCommand(DateTimeOffset? CutoffTime = null) : IRequest<int>;
