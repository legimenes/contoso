namespace Adapters.Identity.AppClients;

public sealed record CreatedAppClientCredential(
    string ClientId,
    string ClientSecret);