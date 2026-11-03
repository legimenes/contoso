namespace Domain.Core.Exceptions;

public sealed class DuplicateAppClientException(Guid merchantId)
    : InvalidOperationException($"An app client for merchant '{merchantId}' already exists.")
{
    public Guid MerchantId { get; } = merchantId;
}