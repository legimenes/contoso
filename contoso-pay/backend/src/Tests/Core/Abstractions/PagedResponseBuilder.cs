using Core.Abstractions.Pagination;

namespace Tests.Core.Abstractions;

public sealed class PagedResponseBuilder<T>
{
    private int? _page;
    private int? _pageSize;
    private int? _totalItems;
    private int? _totalPages;
    private IReadOnlyList<T> _items = [];

    public PagedResponseBuilder<T> WithPage(int page)
    {
        _page = page;
        return this;
    }

    public PagedResponseBuilder<T> WithPageSize(int pageSize)
    {
        _pageSize = pageSize;
        return this;
    }

    public PagedResponseBuilder<T> WithTotalItems(int totalItems)
    {
        _totalItems = totalItems;
        return this;
    }

    public PagedResponseBuilder<T> WithTotalPages(int totalPages)
    {
        _totalPages = totalPages;
        return this;
    }

    public PagedResponseBuilder<T> WithItems(IReadOnlyList<T> items)
    {
        _items = items;
        return this;
    }

    public PagedResponse<T> Build()
    {
        return new PagedResponse<T>(
            Page: _page ?? PaginationRequest.PageDefault,
            PageSize: _pageSize ?? PaginationRequest.PageSizeDefault,
            TotalItems: _totalItems ?? 0,
            TotalPages: _totalPages ?? 0,
            Items: _items);
    }
}
