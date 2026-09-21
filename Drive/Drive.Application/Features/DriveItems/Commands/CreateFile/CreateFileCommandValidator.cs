using FluentValidation;

namespace Drive.Application.Features.DriveItems.Commands.CreateFile;

public sealed class CreateFileCommandValidator
    : AbstractValidator<CreateFileCommand>
{
    private const long MaxFileSize = 100 * 1024 * 1024;

    public CreateFileCommandValidator()
    {
        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("File is required.");

        When(x => x.File != null, () =>
        {
            RuleFor(x => x.File.FileName)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                    .WithMessage("File name is required.")
                .MaximumLength(255)
                    .WithMessage("File name must not exceed 255 characters.")
                .Must(name => !string.IsNullOrWhiteSpace(name))
                    .WithMessage("File name must not be empty.");

            RuleFor(x => x.File.Length)
                .GreaterThan(0)
                .LessThanOrEqualTo(MaxFileSize)
                .WithMessage("File size must be between 1 byte and 100 MB.");

            RuleFor(x => x.File.ContentType)
                .NotEmpty()
                .MaximumLength(255);
        });
    }
}