using Drive.Application.Common.Interfaces;
using Drive.Application.Features.Auth.Models;
using MediatR;

namespace Drive.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler
    : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly IIdentityService _identityService;

    public RegisterCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<RegisterResult> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _identityService.RegisterAsync(
            request.Email,
            request.Password,
            request.DisplayName,
            cancellationToken);

        if (!result.Succeeded)
        {
            var isEmailConflict = result.Errors
                .Contains("Email is already registered.");

            return new RegisterResult
            {
                Succeeded = false,
                IsEmailConflict = isEmailConflict,
                Errors = result.Errors
            };
        }

        return new RegisterResult
        {
            Succeeded = true,
            UserId = result.UserId
        };
    }
}