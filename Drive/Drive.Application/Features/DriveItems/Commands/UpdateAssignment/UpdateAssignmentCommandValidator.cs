using FluentValidation;

namespace Drive.Application.Features.DriveItems.Commands.UpdateAssignment;

public sealed class UpdateAssignmentCommandValidator
    : AbstractValidator<UpdateAssignmentCommand>
{
    public UpdateAssignmentCommandValidator()
    {
        RuleFor(x => x.DriveItemId)
            .NotEmpty()
                .WithMessage("Drive item ID is required.");

        RuleFor(x => x.TargetUserId)
            .NotEmpty()
                .WithMessage("Target user ID is required.");

        RuleFor(x => x.NewRoleId)
            .NotEmpty()
                .WithMessage("New role ID is required.");
    }
}
