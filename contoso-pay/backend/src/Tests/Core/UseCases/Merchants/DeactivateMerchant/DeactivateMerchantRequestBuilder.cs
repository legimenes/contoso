using Core.UseCases.Merchants.DeactivateMerchant;

namespace Tests.Core.UseCases.Merchants.DeactivateMerchant;

public sealed class DeactivateMerchantRequestBuilder
{
    private Guid _id = Guid.NewGuid();

    public DeactivateMerchantRequestBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public DeactivateMerchantRequest Build()
    {
        return new DeactivateMerchantRequest(_id);
    }
}