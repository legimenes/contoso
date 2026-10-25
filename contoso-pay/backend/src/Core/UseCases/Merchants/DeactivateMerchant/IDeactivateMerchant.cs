using Domain.Core.Results;

namespace Core.UseCases.Merchants.DeactivateMerchant;

public interface IDeactivateMerchant
{
    Task<Result> ExecuteAsync(DeactivateMerchantRequest request, CancellationToken cancellationToken);
}