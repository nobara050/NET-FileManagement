using Drive.Application.Common.Authorization;
using FluentValidation;

namespace Drive.Application.Features.Roles.Commands.AddRoleClaim;

public sealed class AddRoleClaimCommandValidator
    : AbstractValidator<AddRoleClaimCommand>
{
    private static readonly string[] AllowedClaims =
    [
        Permissions.DriveRead,
        Permissions.DriveDownload,
        Permissions.DriveCreate,
        Permissions.DriveUpdate,
        Permissions.DriveDelete,
        Permissions.DriveMove,
    ];

    public AddRoleClaimCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty()
                .WithMessage("Role ID is required.");

        RuleFor(x => x.ClaimValue)
            .NotEmpty()
                .WithMessage("Claim value is required.")
            .Must(v => AllowedClaims.Contains(v))
                .WithMessage(
                    "Claim value must be one of the known permissions: " +
                    string.Join(", ", AllowedClaims));
    }
}
