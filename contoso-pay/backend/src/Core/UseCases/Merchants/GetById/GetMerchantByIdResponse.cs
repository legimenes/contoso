namespace Core.UseCases.Merchants.GetById;

public sealed record GetMerchantByIdResponse(
    Guid Id,
    string Name,
    string Document);