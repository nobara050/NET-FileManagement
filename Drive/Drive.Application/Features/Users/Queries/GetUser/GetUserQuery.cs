using Drive.Application.Features.Users.Models;
using MediatR;

namespace Drive.Application.Features.Users.Queries.GetUser;

public sealed record GetUserQuery(Guid UserId) : IRequest<UserResult?>;
