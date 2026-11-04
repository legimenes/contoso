using Core.UseCases.AppClients.CreateAppClient;

namespace Tests.Core.UseCases.AppClients.CreateAppClient;

public sealed class CreateAppClientRequestBuilder
{
    private Guid _merchantId = Guid.NewGuid();
    private string _name = "Loja Exemplo";

    public CreateAppClientRequestBuilder WithMerchantId(Guid merchantId)
    {
        _merchantId = merchantId;
        return this;
    }

    public CreateAppClientRequestBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CreateAppClientRequest Build()
    {
        return new CreateAppClientRequest(_merchantId, _name);
    }
}
