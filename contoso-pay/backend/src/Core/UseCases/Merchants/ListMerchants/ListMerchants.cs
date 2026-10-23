using Core.Abstractions.Pagination;
using Domain.Core.Results;
using FluentValidation;
using FluentValidation.Results;

namespace Core.UseCases.Merchants.ListMerchants;

public sealed class ListMerchants(
    IValidator<ListMerchantsRequest> validator,
    IMerchantQueries queries) : IListMerchants
{
    public async Task<Result<PagedResponse<ListMerchantsResponse>>> ExecuteAsync(ListMerchantsRequest request, CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<PagedResponse<ListMerchantsResponse>>(validationResult.ToDictionary(), ResultType.BadRequest);
        }

        ListMerchantsRequest normalizedRequest = request with
        {
            Document = request.Document?.Trim(),
            Name = request.Name?.Trim()
        };

        PagedResponse<ListMerchantsResponse> merchants = await queries.SearchAsync(normalizedRequest, cancellationToken);

        return Result.Success<PagedResponse<ListMerchantsResponse>>(merchants, ResultType.Ok);
    }
}