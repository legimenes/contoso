using Domain;

namespace Tests.Domain;

public sealed class MerchantBuilder
{
    private string _name = "Loja Exemplo";
    private string _document = "12345678000190";
    private bool _isActive = true;

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

    public MerchantBuilder Inactive()
    {
        _isActive = false;
        return this;
    }

    public Merchant Build()
    {
        Merchant merchant = Merchant.New(
            name: _name,
            document: _document);

        if (!_isActive)
        {
            merchant.Deactivate();
        }

        return merchant;
    }
}