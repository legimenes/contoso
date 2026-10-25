using Domain;
using Domain.Core.Results;
using Domain.Resources;

namespace Core.UseCases.Merchants.ActivateMerchant;

public sealed class ActivateMerchant(IMerchantRepository repository) : IActivateMerchant
{
    public async Task<Result> ExecuteAsync(ActivateMerchantRequest request, CancellationToken cancellationToken)
    {
        Merchant? merchant = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (merchant is null)
        {
            return Result.Failure(Error.New(Resource.Validation_Merchant_NotFound), ResultType.NotFound);
        }

        merchant.Activate();

        await repository.UpdateAsync(merchant, cancellationToken);

        return Result.Success(ResultType.NoContent);
    }
}