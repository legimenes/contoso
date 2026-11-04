namespace Core.Abstractions.Identity;

public sealed record AppClientCredential(
    string ClientId,
    Guid MerchantId,
    string? DisplayName);
