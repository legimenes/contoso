using Core.Abstractions.Pagination;

namespace Core.UseCases.Merchants.ListMerchants;

public sealed record ListMerchantsRequest(
    string? Document,
    string? Name,
    int Page = PaginationRequest.PageDefault,
    int PageSize = PaginationRequest.PageSizeDefault) : PaginationRequest(Page, PageSize);