namespace Core.UseCases.Merchants.CreateMerchant;

public sealed record CreateMerchantRequest(
    string Name,
    string Document);