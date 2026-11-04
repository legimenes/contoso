using Domain.Core.Results;

namespace Core.UseCases.AppClients.GetAppClient;

public interface IGetAppClient
{
    Task<Result<GetAppClientResponse>> ExecuteAsync(GetAppClientRequest request, CancellationToken cancellationToken);
}
