namespace Core.UseCases.Merchants.List;

public sealed record ListMerchantsResponse(
    Guid Id,
    string Name,
    string Document);