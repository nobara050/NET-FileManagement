using Drive.Application.Features.Auth.Models;
using MediatR;

namespace Drive.Application.Features.Auth.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<CurrentUserResult?>;
