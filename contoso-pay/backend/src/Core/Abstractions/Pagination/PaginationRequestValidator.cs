using Domain.Core.Extensions;
using Domain.Resources;
using FluentValidation;

namespace Core.Abstractions.Pagination;

public sealed class PaginationRequestValidator : AbstractValidator<PaginationRequest>
{
    private const int PageMinimum = 1;
    private const int PageSizeMinimum = 1;
    private const int PageSizeMaximum = 100;

    public PaginationRequestValidator()
    {
        RuleFor(static request => request.Page)
            .GreaterThanOrEqualTo(PageMinimum)
            .WithMessage(Resource.Validation_Page_Minimum.Format(PageMinimum));

        RuleFor(static request => request.PageSize)
            .InclusiveBetween(PageSizeMinimum, PageSizeMaximum)
            .WithMessage(Resource.Validation_PageSize_Range.Format(PageSizeMinimum, PageSizeMaximum));
    }
}