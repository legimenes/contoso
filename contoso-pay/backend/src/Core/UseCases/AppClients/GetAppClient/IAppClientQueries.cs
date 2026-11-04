using Domain.Core.Results;

namespace Core.UseCases.AppClients.GetAppClient;

public interface IAppClientQueries
{
    Task<GetAppClientResponse?> GetByClientIdAsync(GetAppClientRequest request, CancellationToken cancellationToken);
}
