using Core.UseCases.Merchants.GetMerchantById;

namespace Tests.Core.UseCases.Merchants.GetMerchantById;

public sealed class GetMerchantByIdRequestBuilder
{
    private Guid _id = Guid.NewGuid();

    public GetMerchantByIdRequestBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public GetMerchantByIdRequest Build()
    {
        return new GetMerchantByIdRequest(_id);
    }
}