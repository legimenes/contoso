using Core.UseCases.Merchants.ActivateMerchant;

namespace Tests.Core.UseCases.Merchants.ActivateMerchant;

public sealed class ActivateMerchantRequestBuilder
{
    private Guid _id = Guid.NewGuid();

    public ActivateMerchantRequestBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public ActivateMerchantRequest Build()
    {
        return new ActivateMerchantRequest(_id);
    }
}