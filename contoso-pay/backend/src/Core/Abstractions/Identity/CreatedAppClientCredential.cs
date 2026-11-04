namespace Core.Abstractions.Identity;

public sealed record CreatedAppClientCredential(
    string ClientId,
    string ClientSecret);
