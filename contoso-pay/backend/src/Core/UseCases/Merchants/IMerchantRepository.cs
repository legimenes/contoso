using Domain;

namespace Core.UseCases.Merchants;

public interface IMerchantRepository
{
    Task AddAsync(Merchant merchant, CancellationToken cancellationToken);
    Task<bool> ExistsByDocumentAsync(string document, CancellationToken cancellationToken);
}