namespace Core.UseCases.AppClients.GetAppClient;

public sealed record GetAppClientResponse(
    string ClientId,
    Guid MerchantId,
    string Name);
