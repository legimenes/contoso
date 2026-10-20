using Domain.Core.Results;

namespace Core.UseCases.Merchants.GetById;

public interface IGetMerchantById
{
    Task<Result<GetMerchantByIdResponse>> ExecuteAsync(GetMerchantByIdRequest request, CancellationToken cancellationToken);
}