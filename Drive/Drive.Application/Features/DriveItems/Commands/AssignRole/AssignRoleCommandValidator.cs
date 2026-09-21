using FluentValidation;

namespace Drive.Application.Features.DriveItems.Commands.AssignRole;

public class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(x => x.DriveItemId)
            .NotEmpty()
            .WithMessage("Drive item ID is required.");

        RuleFor(x => x)
            .Must(x => (x.TargetUserId.HasValue && x.TargetUserId.Value != Guid.Empty) || !string.IsNullOrWhiteSpace(x.TargetEmail))
            .WithMessage("Either target user ID or target email must be provided.");

        RuleFor(x => x.RoleId)
            .NotEmpty()
            .WithMessage("Role ID is required.");
    }
}
