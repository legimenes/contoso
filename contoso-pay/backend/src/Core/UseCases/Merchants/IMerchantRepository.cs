using Domain;

namespace Core.UseCases.Merchants;

public interface IMerchantRepository
{
    Task AddAsync(Merchant merchant, CancellationToken cancellationToken);
    Task<bool> ExistsByDocumentAsync(string document, CancellationToken cancellationToken);
    Task<Merchant?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(Merchant merchant, CancellationToken cancellationToken);
}