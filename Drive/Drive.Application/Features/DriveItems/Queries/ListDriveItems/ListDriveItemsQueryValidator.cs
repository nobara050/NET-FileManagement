using FluentValidation;

namespace Drive.Application.Features.DriveItems.Queries.ListDriveItems;

public sealed class ListDriveItemsQueryValidator
    : AbstractValidator<ListDriveItemsQuery>
{
    public ListDriveItemsQueryValidator()
    {
        RuleFor(x => x.SearchTerm)
            .MaximumLength(255)
            .When(x => x.SearchTerm is not null);

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);
    }
}