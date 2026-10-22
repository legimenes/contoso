namespace Core.Abstractions.Pagination;

public abstract record PaginationRequest(
    int Page = PaginationRequest.PageDefault,
    int PageSize = PaginationRequest.PageSizeDefault)
{
    public const int PageDefault = 1;
    public const int PageSizeDefault = 20;
}