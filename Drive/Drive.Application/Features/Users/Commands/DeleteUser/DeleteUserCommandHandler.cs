using Drive.Application.Common.Interfaces;
using MediatR;

namespace Drive.Application.Features.Users.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler
    : IRequestHandler<DeleteUserCommand, bool>
{
    private readonly IUserManagementService _userManagementService;

    public DeleteUserCommandHandler(
        IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public async Task<bool> Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken)
    {
        return await _userManagementService.DeleteUserAsync(
            request.UserId,
            cancellationToken);
    }
}
