using Drive.Application.Common.Authorization;
using Drive.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Drive.Api.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionAuthorizationHandler(IPermissionService permissionService, ICurrentUserService currentUserService, IHttpContextAccessor httpContextAccessor)
    {
        _permissionService = permissionService;
        _currentUserService = currentUserService;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var userId = _currentUserService.UserId;

        if (userId is null)
        {
            context.Fail();
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            context.Fail();
            return;
        }

        var driveItemId = ResolveTargetItemId(httpContext);

        if (driveItemId is null)
        {
            // Fail-Closed: If an endpoint targets an item via route but the ID is missing/invalid, deny access.
            if (httpContext.Request.RouteValues.ContainsKey("driveItemId") ||
                httpContext.Request.RouteValues.ContainsKey("itemId") ||
                httpContext.Request.RouteValues.ContainsKey("id"))
            {
                context.Fail();
                return;
            }

            // If parentId parameter was supplied in query/form but was empty or malformed GUID, deny access.
            if (httpContext.Request.Query.ContainsKey("parentId") &&
                !string.IsNullOrWhiteSpace(httpContext.Request.Query["parentId"]))
            {
                context.Fail();
                return;
            }

            if (httpContext.Request.HasFormContentType &&
                httpContext.Request.Form.ContainsKey("parentId") &&
                !string.IsNullOrWhiteSpace(httpContext.Request.Form["parentId"]))
            {
                context.Fail();
                return;
            }

            // Root-level operations with no parentId (e.g., listing root or creating at root)
            // are allowed for any authenticated user.
            context.Succeed(requirement);
            return;
        }

        var allowed = await _permissionService.HasPermissionAsync(
            userId.Value,
            driveItemId.Value,
            requirement.Permission);

        if (allowed)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }

    private static Guid? ResolveTargetItemId(HttpContext httpContext)
    {
        // 1. Route values: e.g. {driveItemId:guid}, {itemId:guid}, {id:guid}
        string[] routeKeys = ["driveItemId", "itemId", "id"];
        foreach (var key in routeKeys)
        {
            if (httpContext.Request.RouteValues.TryGetValue(key, out var routeVal) &&
                Guid.TryParse(routeVal?.ToString(), out var fromRoute))
            {
                return fromRoute;
            }
        }

        // 2. Query string: GET /api/drive-items?parentId=...
        if (httpContext.Request.Query.TryGetValue("parentId", out StringValues queryValue) &&
            Guid.TryParse(queryValue.FirstOrDefault(), out var fromQuery))
        {
            return fromQuery;
        }

        // 3. Form field: POST /api/drive-items/files (multipart/form-data)
        if (httpContext.Request.HasFormContentType &&
            httpContext.Request.Form.TryGetValue("parentId", out StringValues formValue) &&
            Guid.TryParse(formValue.FirstOrDefault(), out var fromForm))
        {
            return fromForm;
        }

        return null;
    }
}
