namespace Core.UseCases.Merchants.GetMerchantById;

public sealed record GetMerchantByIdResponse(
    Guid Id,
    string Name,
    string Document,
    bool IsActive);