using Adapters.Identity.AppClients;

namespace Tests.Core.Abstractions.Identity;

public sealed class AppClientCredentialBuilder
{
    private string _clientId = Guid.NewGuid().ToString("N");
    private Guid _merchantId = Guid.NewGuid();
    private string? _displayName = "Loja Exemplo";

    public AppClientCredentialBuilder WithClientId(string clientId)
    {
        _clientId = clientId;
        return this;
    }

    public AppClientCredentialBuilder WithMerchantId(Guid merchantId)
    {
        _merchantId = merchantId;
        return this;
    }

    public AppClientCredentialBuilder WithDisplayName(string? displayName)
    {
        _displayName = displayName;
        return this;
    }

    public AppClientCredential Build()
    {
        return new AppClientCredential(
            ClientId: _clientId,
            MerchantId: _merchantId,
            DisplayName: _displayName);
    }
}