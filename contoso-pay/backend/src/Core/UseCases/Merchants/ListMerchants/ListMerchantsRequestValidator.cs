using Core.Abstractions.Pagination;
using FluentValidation;

namespace Core.UseCases.Merchants.ListMerchants;

public sealed class ListMerchantsRequestValidator : AbstractValidator<ListMerchantsRequest>
{
    public ListMerchantsRequestValidator()
    {
        Include(new PaginationRequestValidator());
    }
}