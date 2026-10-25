using Domain;
using Domain.Core.Results;
using Domain.Resources;

namespace Core.UseCases.Merchants.GetMerchantById;

public sealed class GetMerchantById(IMerchantRepository repository) : IGetMerchantById
{
    public async Task<Result<GetMerchantByIdResponse>> ExecuteAsync(GetMerchantByIdRequest request, CancellationToken cancellationToken)
    {
        Merchant? merchant = await repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (merchant is null)
        {
            return Result.Failure<GetMerchantByIdResponse>(Error.New(Resource.Validation_Merchant_NotFound), ResultType.NotFound);
        }

        return Result.Success<GetMerchantByIdResponse>(new GetMerchantByIdResponse(merchant.Id, merchant.Name, merchant.Document, merchant.IsActive), ResultType.Ok);
    }
}