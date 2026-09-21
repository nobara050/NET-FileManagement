using Drive.Application.Common.Interfaces;
using Drive.Application.Features.Users.Models;
using MediatR;

namespace Drive.Application.Features.Users.Queries.ListUsers;

public sealed class ListUsersQueryHandler
    : IRequestHandler<ListUsersQuery, IReadOnlyList<UserResult>>
{
    private readonly IUserManagementService _userManagementService;

    public ListUsersQueryHandler(
        IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public async Task<IReadOnlyList<UserResult>> Handle(
        ListUsersQuery request,
        CancellationToken cancellationToken)
    {
        return await _userManagementService.ListUsersAsync(
            cancellationToken);
    }
}
