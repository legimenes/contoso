using Core.UseCases.Merchants;
using Domain;
using Domain.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Adapters.Persistence.Repositories;

public sealed class MerchantRepository(ContosoPayDbContext dbContext) : IMerchantRepository
{
    public async Task AddAsync(Merchant merchant, CancellationToken cancellationToken)
    {
        await dbContext.Set<Merchant>().AddAsync(merchant, cancellationToken).ConfigureAwait(false);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new DuplicateMerchantDocumentException(merchant.Document);
        }
    }

    public Task<bool> ExistsByDocumentAsync(string document, CancellationToken cancellationToken)
    {
        return dbContext.Set<Merchant>().AnyAsync(merchant => merchant.Document == document, cancellationToken);
    }

    public Task<Merchant?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Set<Merchant>().FirstOrDefaultAsync(merchant => merchant.Id == id, cancellationToken);
    }
}