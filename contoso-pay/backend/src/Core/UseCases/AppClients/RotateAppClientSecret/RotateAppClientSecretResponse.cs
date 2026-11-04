namespace Core.UseCases.AppClients.RotateAppClientSecret;

public sealed record RotateAppClientSecretResponse(string ClientId, string ClientSecret);
