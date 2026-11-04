namespace Core.UseCases.AppClients.RotateAppClientSecret;

public sealed record RotateAppClientSecretRequest(string ClientId, Guid MerchantId, string CurrentSecret);
