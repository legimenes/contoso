namespace Core.UseCases.AppClients.CreateAppClient;

public sealed record CreateAppClientRequest(Guid MerchantId, string Name);
