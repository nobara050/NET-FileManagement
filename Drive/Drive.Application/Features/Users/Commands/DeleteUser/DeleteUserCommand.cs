using MediatR;

namespace Drive.Application.Features.Users.Commands.DeleteUser;

public sealed record DeleteUserCommand(Guid UserId) : IRequest<bool>;
