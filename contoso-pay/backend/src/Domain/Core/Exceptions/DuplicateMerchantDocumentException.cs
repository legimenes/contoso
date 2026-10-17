namespace Domain.Core.Exceptions;

public sealed class DuplicateMerchantDocumentException(string document)
    : InvalidOperationException($"Merchant with document '{document}' already exists.")
{
    public string Document { get; } = document;
}