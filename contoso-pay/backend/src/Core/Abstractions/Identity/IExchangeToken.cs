using Domain.Core.Results;
using System.Security.Claims;

namespace Core.Abstractions.Identity;

public interface IExchangeToken
{
    Task<Result<ClaimsPrincipal>> ExchangeAsync(string clientId, string clientSecret, CancellationToken cancellationToken);
}