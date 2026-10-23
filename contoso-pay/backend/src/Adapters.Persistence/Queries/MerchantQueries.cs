using Core.Abstractions.Pagination;
using Core.UseCases.Merchants;
using Core.UseCases.Merchants.ListMerchants;
using Domain;
using Microsoft.EntityFrameworkCore;

namespace Adapters.Persistence.Queries;

public sealed class MerchantQueries(ContosoPayDbContext dbContext) : IMerchantQueries
{
    public async Task<PagedResponse<ListMerchantsResponse>> SearchAsync(ListMerchantsRequest request, CancellationToken cancellationToken)
    {
        IQueryable<Merchant> query = dbContext.Set<Merchant>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Document))
        {
            query = query.Where(merchant => merchant.Document == request.Document);
        }

        string? namePrefix = request.Name;
        if (!string.IsNullOrWhiteSpace(namePrefix))
        {
            string escapedPrefix = EscapeLikePattern(namePrefix);
            query = query.Where(merchant => EF.Functions.ILike(merchant.Name, escapedPrefix + "%"));
        }

        int totalItems = await query.CountAsync(cancellationToken);
        int totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)request.PageSize);

        IReadOnlyList<ListMerchantsResponse> merchants = await query
            .OrderBy(merchant => merchant.Name.ToLower())
            .ThenBy(merchant => merchant.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(merchant => new ListMerchantsResponse(merchant.Id, merchant.Name, merchant.Document))
            .ToListAsync(cancellationToken);

        return new PagedResponse<ListMerchantsResponse>(request.Page, request.PageSize, totalItems, totalPages, merchants);
    }

    private static string EscapeLikePattern(string input)
    {
        return input
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
    }
}