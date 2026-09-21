using Drive.Application.Common.Interfaces;
using Drive.Application.Features.Users.Models;
using MediatR;

namespace Drive.Application.Features.Users.Queries.GetUser;

public sealed class GetUserQueryHandler
    : IRequestHandler<GetUserQuery, UserResult?>
{
    private readonly IUserManagementService _userManagementService;

    public GetUserQueryHandler(
        IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public async Task<UserResult?> Handle(
        GetUserQuery request,
        CancellationToken cancellationToken)
    {
        return await _userManagementService.GetUserByIdAsync(
            request.UserId,
            cancellationToken);
    }
}
