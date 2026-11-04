using Core.UseCases.AppClients.GetAppClient;

namespace Tests.Core.UseCases.AppClients.GetAppClient;

public sealed class GetAppClientRequestBuilder
{
    private string _clientId = Guid.NewGuid().ToString("N");
    private Guid _merchantId = Guid.NewGuid();

    public GetAppClientRequestBuilder WithClientId(string clientId)
    {
        _clientId = clientId;
        return this;
    }

    public GetAppClientRequestBuilder WithMerchantId(Guid merchantId)
    {
        _merchantId = merchantId;
        return this;
    }

    public GetAppClientRequest Build()
    {
        return new GetAppClientRequest(_clientId, _merchantId);
    }
}
