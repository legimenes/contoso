using Domain.Core.Results;

namespace Core.UseCases.Merchants.GetMerchantById;

public interface IGetMerchantById
{
    Task<Result<GetMerchantByIdResponse>> ExecuteAsync(GetMerchantByIdRequest request, CancellationToken cancellationToken);
}