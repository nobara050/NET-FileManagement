using Drive.Application.Common.Interfaces;
using Drive.Application.Features.Users.Models;
using MediatR;

namespace Drive.Application.Features.Users.Queries.SearchUsers;

public sealed class SearchUsersQueryHandler : IRequestHandler<SearchUsersQuery, IReadOnlyList<UserResult>>
{
    private readonly IUserManagementService _userManagementService;
    private readonly ICurrentUserService _currentUserService;

    public SearchUsersQueryHandler(
        IUserManagementService userManagementService,
        ICurrentUserService currentUserService)
    {
        _userManagementService = userManagementService;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<UserResult>> Handle(
        SearchUsersQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;

        return await _userManagementService.SearchUsersAsync(
            request.Query,
            request.Limit,
            currentUserId,
            cancellationToken);
    }
}
