using Domain.Core.Results;

namespace Core.UseCases.AppClients.RotateAppClientSecret;

public interface IRotateAppClientSecret
{
    Task<Result<RotateAppClientSecretResponse>> ExecuteAsync(RotateAppClientSecretRequest request, CancellationToken cancellationToken);
}
