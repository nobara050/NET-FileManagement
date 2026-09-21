using FluentValidation;

namespace Drive.Application.Features.DriveItems.Queries.GetDownloadUrl;

public sealed class GetDownloadUrlQueryValidator
    : AbstractValidator<GetDownloadUrlQuery>
{
    public GetDownloadUrlQueryValidator()
    {
        RuleFor(x => x.DriveItemId)
            .NotEmpty()
            .WithMessage("DriveItem ID is required.");
    }
}
