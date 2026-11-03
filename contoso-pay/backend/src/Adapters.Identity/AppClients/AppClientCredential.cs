namespace Adapters.Identity.AppClients;

public sealed record AppClientCredential(
    string ClientId,
    Guid MerchantId,
    string? DisplayName);