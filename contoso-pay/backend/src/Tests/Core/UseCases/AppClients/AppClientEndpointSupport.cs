using Adapters.Identity;
using Core.Abstractions.Identity;
using Core.UseCases.Merchants;
using Domain;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Tests.Domain;

namespace Tests.Core.UseCases.AppClients;

internal static class AppClientEndpointSupport
{
    internal static async Task<Guid> CreateMerchantAsync(WebApplicationFactory<Program> factory, bool isActive = true)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        IMerchantRepository repository = scope.ServiceProvider.GetRequiredService<IMerchantRepository>();
        Merchant merchant = isActive
            ? new MerchantBuilder().WithDocument(NewDocument()).Build()
            : new MerchantBuilder().WithDocument(NewDocument()).Inactive().Build();
        await repository.AddAsync(merchant, CancellationToken.None);
        return merchant.Id;
    }

    private static string NewDocument()
    {
        return Random.Shared.NextInt64(10_000_000_000L, 100_000_000_000_000L).ToString();
    }

    internal static async Task<CreatedAppClientCredential> CreateAppClientAsync(
        WebApplicationFactory<Program> factory,
        Guid merchantId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        IAppClientProvider appClients = scope.ServiceProvider.GetRequiredService<IAppClientProvider>();
        return await appClients.CreateAsync(merchantId, "Loja Exemplo", CancellationToken.None);
    }

    internal static async Task DeleteMerchantAsync(string connectionString, Guid merchantId)
    {
        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(
            """
            DELETE FROM contosopay.app_client_tokens
            WHERE application_id IN (SELECT id FROM contosopay.app_clients WHERE merchant_id = @merchantId);

            DELETE FROM contosopay.app_clients WHERE merchant_id = @merchantId;
            DELETE FROM contosopay.merchants WHERE id = @merchantId;
            """,
            connection);
        command.Parameters.AddWithValue("merchantId", merchantId);
        await command.ExecuteNonQueryAsync();
    }
}
