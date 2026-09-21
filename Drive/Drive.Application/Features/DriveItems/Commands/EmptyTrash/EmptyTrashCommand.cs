using MediatR;

namespace Drive.Application.Features.DriveItems.Commands.EmptyTrash;

public sealed record EmptyTrashCommand : IRequest<int>;
