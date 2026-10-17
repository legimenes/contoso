using Domain.Core.Results;

namespace Core.UseCases.Merchants.Create;

public interface ICreateMerchant
{
    Task<Result<Guid>> ExecuteAsync(CreateMerchantRequest request, CancellationToken cancellationToken);
}