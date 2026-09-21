using FluentValidation;

namespace Drive.Application.Features.DriveItems.Commands.RenameDriveItem;

public sealed class RenameDriveItemCommandValidator : AbstractValidator<RenameDriveItemCommand>
{
    private static readonly char[] InvalidChars = { '/', '\\', ':', '*', '?', '"', '<', '>', '|' };

    public RenameDriveItemCommandValidator()
    {
        RuleFor(x => x.DriveItemId)
            .NotEmpty().WithMessage("Drive item ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(255).WithMessage("Name must not exceed 255 characters.")
            .Must(name => name != null && !name.Any(c => InvalidChars.Contains(c)))
            .WithMessage("Name contains invalid characters.");
    }
}
