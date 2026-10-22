using Core.Abstractions.Pagination;
using Core.UseCases.Merchants.List;

namespace Tests.Core.Merchants.List;

public sealed class ListMerchantsRequestBuilder
{
    private string? _document;
    private string? _name;
    private int? _page;
    private int? _pageSize;

    public ListMerchantsRequestBuilder WithDocument(string document)
    {
        _document = document;
        return this;
    }

    public ListMerchantsRequestBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ListMerchantsRequestBuilder WithPage(int page)
    {
        _page = page;
        return this;
    }

    public ListMerchantsRequestBuilder WithPageSize(int pageSize)
    {
        _pageSize = pageSize;
        return this;
    }

    public ListMerchantsRequest Build()
    {
        return new ListMerchantsRequest(
            Document: _document,
            Name: _name,
            Page: _page ?? PaginationRequest.PageDefault,
            PageSize: _pageSize ?? PaginationRequest.PageSizeDefault);
    }
}