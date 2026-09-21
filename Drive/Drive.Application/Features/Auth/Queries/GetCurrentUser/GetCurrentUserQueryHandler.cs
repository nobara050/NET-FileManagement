using Drive.Application.Common.Interfaces;
using Drive.Application.Features.Auth.Models;
using MediatR;

namespace Drive.Application.Features.Auth.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserResult?>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserManagementService _userManagementService;
    private readonly IIdentityService _identityService;

    public GetCurrentUserQueryHandler(
        ICurrentUserService currentUserService,
        IUserManagementService userManagementService,
        IIdentityService identityService)
    {
        _currentUserService = currentUserService;
        _userManagementService = userManagementService;
        _identityService = identityService;
    }

    public async Task<CurrentUserResult?> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return null;
        }

        var user = await _userManagementService.GetUserByIdAsync(
            userId.Value,
            cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roles = await _identityService.GetRolesAsync(
            userId.Value,
            cancellationToken);

        return new CurrentUserResult
        {
            UserId = user.UserId,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            Roles = roles
        };
    }
}
