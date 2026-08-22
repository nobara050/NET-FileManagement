using FluentValidation;

namespace Drive.Application.Features.DriveItems.Commands.CreateFolder;

public sealed class CreateFolderCommandValidator
    : AbstractValidator<CreateFolderCommand>
{
    public CreateFolderCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255)
            .Must(name => name.Trim().Length > 0)
            .WithMessage("Folder name must not be empty.");
    }
}