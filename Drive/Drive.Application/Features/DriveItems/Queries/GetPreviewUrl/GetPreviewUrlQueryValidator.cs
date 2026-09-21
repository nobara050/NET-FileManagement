using FluentValidation;

namespace Drive.Application.Features.DriveItems.Queries.GetPreviewUrl;

public sealed class GetPreviewUrlQueryValidator
    : AbstractValidator<GetPreviewUrlQuery>
{
    public GetPreviewUrlQueryValidator()
    {
        RuleFor(x => x.DriveItemId)
            .NotEmpty()
            .WithMessage("DriveItem ID is required.");
    }
}
