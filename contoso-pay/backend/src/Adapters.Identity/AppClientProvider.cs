using Adapters.Identity.AppClients;
using Adapters.Identity.Models;
using Core.Abstractions.Identity;
using Domain.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenIddict.Abstractions;
using System.Buffers.Text;
using System.Security.Cryptography;

namespace Adapters.Identity;

public sealed class AppClientProvider(
    AppClientManager manager,
    IdentityDbContext dbContext) : IAppClientProvider
{
    private const int SecretLengthInBytes = 32;

    public Task<bool> ExistsByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken)
    {
        return dbContext.Set<AppClient>().AnyAsync(appClient => appClient.MerchantId == merchantId, cancellationToken);
    }

    public async Task<CreatedAppClientCredential> CreateAsync(
        Guid merchantId,
        string displayName,
        CancellationToken cancellationToken)
    {
        string clientId = Guid.NewGuid().ToString("N");
        string clientSecret = GenerateSecret();
        AppClientDescriptor descriptor = new()
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            DisplayName = displayName,
            MerchantId = merchantId
        };
        descriptor.Permissions.UnionWith(
            [
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.ClientCredentials
            ]);

        try
        {
            await manager.CreateAsync(descriptor, cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_app_clients_merchant_id"
        })
        {
            throw new DuplicateAppClientException(merchantId);
        }

        return new CreatedAppClientCredential(clientId, clientSecret);
    }

    public async Task<AppClientCredential?> FindByClientIdAsync(string clientId, CancellationToken cancellationToken)
    {
        AppClient? appClient = await manager.FindByClientIdAsync(clientId, cancellationToken);
        if (appClient is null)
        {
            return null;
        }

        return new AppClientCredential(
            ClientId: appClient.ClientId ?? clientId,
            MerchantId: appClient.MerchantId,
            DisplayName: appClient.DisplayName);
    }

    public async Task<bool> ValidateSecretAsync(string clientId, string clientSecret, CancellationToken cancellationToken)
    {
        AppClient? appClient = await manager.FindByClientIdAsync(clientId, cancellationToken);
        if (appClient is null)
        {
            return false;
        }

        return await manager.ValidateClientSecretAsync(appClient, clientSecret, cancellationToken);
    }

    public async Task<CreatedAppClientCredential?> RotateSecretAsync(string clientId, CancellationToken cancellationToken)
    {
        AppClient? appClient = await manager.FindByClientIdAsync(clientId, cancellationToken);
        if (appClient is null)
        {
            return null;
        }

        string clientSecret = GenerateSecret();
        await manager.UpdateAsync(appClient, clientSecret, cancellationToken);

        return new CreatedAppClientCredential(clientId, clientSecret);
    }

    private static string GenerateSecret()
    {
        return Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(SecretLengthInBytes));
    }
}