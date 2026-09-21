using Drive.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Drive.Api.Authorization;

public sealed class AdminAuthorizationHandler : AuthorizationHandler<AdminRequirement>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;

    public AdminAuthorizationHandler(
        ICurrentUserService currentUserService,
        IIdentityService identityService)
    {
        _currentUserService = currentUserService;
        _identityService = identityService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminRequirement requirement)
    {
        var userId = _currentUserService.UserId;

        if (userId is null)
        {
            context.Fail();
            return;
        }

        var roles = await _identityService.GetRolesAsync(userId.Value);

        if (roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
