namespace Core.UseCases.AppClients.GetAppClient;

public sealed record GetAppClientRequest(string ClientId, Guid MerchantId);
