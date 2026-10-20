using Core.UseCases.Merchants.GetById;

namespace Tests.Core.Merchants.GetById;

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