namespace Core.UseCases.Merchants.ListMerchants;

public sealed record ListMerchantsResponse(
    Guid Id,
    string Name,
    string Document);