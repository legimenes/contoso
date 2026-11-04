using Adapters.Identity.Models;
using Core.UseCases.AppClients.GetAppClient;
using Microsoft.EntityFrameworkCore;

namespace Adapters.Identity.Queries;

public sealed class AppClientQueries(IdentityDbContext dbContext) : IAppClientQueries
{
    public Task<GetAppClientResponse?> GetByClientIdAsync(GetAppClientRequest request, CancellationToken cancellationToken)
    {
        return dbContext.Set<AppClient>()
            .Where(appClient => appClient.ClientId == request.ClientId && appClient.MerchantId == request.MerchantId)
            .Select(static appClient => new GetAppClientResponse(
                appClient.ClientId ?? string.Empty,
                appClient.MerchantId,
                appClient.DisplayName ?? string.Empty))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
