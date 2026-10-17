using Domain;

namespace Tests.Domain;

public sealed class MerchantBuilder
{
    private string _name = "Loja Exemplo";
    private string _document = "12345678000190";

    public MerchantBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public MerchantBuilder WithDocument(string document)
    {
        _document = document;
        return this;
    }

    public Merchant Build()
    {
        return Merchant.New(
            name: _name,
            document: _document);
    }
}