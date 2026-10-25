using Domain.Core.Results;

namespace Core.UseCases.Merchants.ActivateMerchant;

public interface IActivateMerchant
{
    Task<Result> ExecuteAsync(ActivateMerchantRequest request, CancellationToken cancellationToken);
}