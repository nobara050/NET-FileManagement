using FluentValidation;

namespace Drive.Application.Features.DriveItems.Commands.CreateFolder;

public sealed class CreateFolderCommandValidator
    : AbstractValidator<CreateFolderCommand>
{
    public CreateFolderCommandValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Folder name is required.")
            .MaximumLength(255)
                .WithMessage("Folder name must not exceed 255 characters.")
            .Must(name => !string.IsNullOrWhiteSpace(name))
                .WithMessage("Folder name must not be empty.");
    }
}