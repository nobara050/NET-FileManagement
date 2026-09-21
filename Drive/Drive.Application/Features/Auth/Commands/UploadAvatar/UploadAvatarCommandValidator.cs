using FluentValidation;

namespace Drive.Application.Features.Auth.Commands.UploadAvatar;

public sealed class UploadAvatarCommandValidator : AbstractValidator<UploadAvatarCommand>
{
    private static readonly string[] AllowedContentTypes =
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    public UploadAvatarCommandValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("Avatar file is required.");

        RuleFor(x => x.File.Length)
            .GreaterThan(0).WithMessage("Avatar file cannot be empty.")
            .LessThanOrEqualTo(5 * 1024 * 1024).WithMessage("Avatar file size must not exceed 5MB.");

        RuleFor(x => x.File.ContentType)
            .Must(ct => AllowedContentTypes.Contains(ct.ToLower()))
            .WithMessage("Avatar must be a JPEG, PNG, WebP, or GIF image.");
    }
}
