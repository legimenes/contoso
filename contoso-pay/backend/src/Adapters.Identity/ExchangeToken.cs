using Core.Abstractions.Identity;
using Core.UseCases.Merchants;
using Domain;
using Domain.Core.Results;
using Domain.Resources;
using OpenIddict.Abstractions;
using System.Security.Claims;

namespace Adapters.Identity;

public sealed class ExchangeToken(
    IAppClientProvider appClients,
    IMerchantRepository merchants) : IExchangeToken
{
    public async Task<Result<ClaimsPrincipal>> ExchangeAsync(string clientId, string clientSecret, CancellationToken cancellationToken)
    {
        AppClientCredential? appClient = await appClients.FindByClientIdAsync(clientId, cancellationToken);
        if (appClient is null)
        {
            return Unauthorized();
        }

        bool secretIsValid = await appClients.ValidateSecretAsync(clientId, clientSecret, cancellationToken);
        if (!secretIsValid)
        {
            return Unauthorized();
        }

        Merchant? merchant = await merchants.GetByIdAsync(appClient.MerchantId, cancellationToken);
        if (merchant is null || !merchant.IsActive)
        {
            return Unauthorized();
        }

        ClaimsIdentity identity = new(
            authenticationType: "Bearer",
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);
        identity.SetClaim(OpenIddictConstants.Claims.Subject, appClient.ClientId);
        identity.SetClaim(IdentityConstants.MerchantIdClaim, merchant.Id.ToString());
        identity.SetResources(IdentityConstants.Audience);
        identity.SetDestinations(static _ => [OpenIddictConstants.Destinations.AccessToken]);

        return Result.Success(new ClaimsPrincipal(identity));
    }

    private static Result<ClaimsPrincipal> Unauthorized()
    {
        return Result.Failure<ClaimsPrincipal>(
            Error.New(Resource.Validation_Token_InvalidClient),
            ResultType.Unauthorized);
    }
}