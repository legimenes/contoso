using Core.UseCases.Merchants.CreateMerchant;

namespace Tests.Core.UseCases.Merchants.CreateMerchant;

public sealed class CreateMerchantRequestBuilder
{
    private string _name = "Loja Exemplo";
    private string _document = "12345678000190";

    public CreateMerchantRequestBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CreateMerchantRequestBuilder WithDocument(string document)
    {
        _document = document;
        return this;
    }

    public CreateMerchantRequest Build()
    {
        return new CreateMerchantRequest(
            Name: _name,
            Document: _document);
    }
}