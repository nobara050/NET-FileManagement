using Drive.Application.Features.Users.Models;
using MediatR;

namespace Drive.Application.Features.Users.Queries.ListUsers;

public sealed record ListUsersQuery() : IRequest<IReadOnlyList<UserResult>>;
