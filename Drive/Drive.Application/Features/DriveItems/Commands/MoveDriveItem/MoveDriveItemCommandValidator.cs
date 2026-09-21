using FluentValidation;

namespace Drive.Application.Features.DriveItems.Commands.MoveDriveItem;

public sealed class MoveDriveItemCommandValidator : AbstractValidator<MoveDriveItemCommand>
{
    public MoveDriveItemCommandValidator()
    {
        RuleFor(x => x.DriveItemId)
            .NotEmpty().WithMessage("Drive item ID is required.");

        RuleFor(x => x)
            .Must(x => !x.TargetParentId.HasValue || x.TargetParentId.Value != x.DriveItemId)
            .WithMessage("Cannot move an item into itself.");
    }
}
