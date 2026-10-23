using Core.Abstractions.Pagination;
using Core.UseCases.Merchants.ListMerchants;

namespace Core.UseCases.Merchants;

public interface IMerchantQueries
{
    Task<PagedResponse<ListMerchantsResponse>> SearchAsync(ListMerchantsRequest request, CancellationToken cancellationToken);
}