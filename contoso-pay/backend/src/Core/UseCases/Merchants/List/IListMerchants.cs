using Core.Abstractions.Pagination;
using Domain.Core.Results;

namespace Core.UseCases.Merchants.List;

public interface IListMerchants
{
    Task<Result<PagedResponse<ListMerchantsResponse>>> ExecuteAsync(ListMerchantsRequest request, CancellationToken cancellationToken);
}