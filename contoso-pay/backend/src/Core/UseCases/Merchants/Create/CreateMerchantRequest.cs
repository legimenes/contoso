namespace Core.UseCases.Merchants.Create;

public sealed record CreateMerchantRequest(
    string Name,
    string Document);