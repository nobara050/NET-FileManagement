using Drive.Application.Features.Users.Models;
using MediatR;

namespace Drive.Application.Features.Users.Queries.SearchUsers;

public sealed record SearchUsersQuery(string Query, int Limit = 10) : IRequest<IReadOnlyList<UserResult>>;
