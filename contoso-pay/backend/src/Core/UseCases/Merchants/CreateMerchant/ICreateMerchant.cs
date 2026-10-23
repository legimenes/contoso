using Domain.Core.Results;

namespace Core.UseCases.Merchants.CreateMerchant;

public interface ICreateMerchant
{
    Task<Result<CreateMerchantResponse>> ExecuteAsync(CreateMerchantRequest request, CancellationToken cancellationToken);
}