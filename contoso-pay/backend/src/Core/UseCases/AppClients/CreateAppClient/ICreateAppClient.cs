using Domain.Core.Results;

namespace Core.UseCases.AppClients.CreateAppClient;

public interface ICreateAppClient
{
    Task<Result<CreateAppClientResponse>> ExecuteAsync(CreateAppClientRequest request, CancellationToken cancellationToken);
}
