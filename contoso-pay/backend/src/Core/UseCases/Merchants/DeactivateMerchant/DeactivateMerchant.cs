using Domain;
using Domain.Core.Results;
using Domain.Resources;

namespace Core.UseCases.Merchants.DeactivateMerchant;

public sealed class DeactivateMerchant(IMerchantRepository repository) : IDeactivateMerchant
{
    public async Task<Result> ExecuteAsync(DeactivateMerchantRequest request, CancellationToken cancellationToken)
    {
        Merchant? merchant = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (merchant is null)
        {
            return Result.Failure(Error.New(Resource.Validation_Merchant_NotFound), ResultType.NotFound);
        }

        merchant.Deactivate();

        await repository.UpdateAsync(merchant, cancellationToken);

        return Result.Success(ResultType.NoContent);
    }
}