using Core.Abstractions.Identity;
using Core.UseCases.AppClients.RotateAppClientSecret;

namespace Tests.Core.UseCases.AppClients.RotateAppClientSecret;

public sealed class RotateAppClientSecretRequestBuilder
{
    private string _clientId = Guid.NewGuid().ToString("N");
    private string _currentSecret = "current-secret";

    public RotateAppClientSecretRequestBuilder WithClientId(string clientId)
    {
        _clientId = clientId;
        return this;
    }

    public RotateAppClientSecretRequestBuilder WithCurrentSecret(string currentSecret)
    {
        _currentSecret = currentSecret;
        return this;
    }

    public RotateAppClientSecretRequest Build()
    {
        return new RotateAppClientSecretRequest(_clientId, Guid.NewGuid(), _currentSecret);
    }
}
