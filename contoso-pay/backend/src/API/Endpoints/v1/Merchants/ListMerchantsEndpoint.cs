using API.Middlewares;
using Core.Abstractions.Pagination;
using Core.UseCases.Merchants.List;
using Domain.Core.Results;

namespace API.Endpoints.v1.Merchants;

// Debt: protected by Bearer when the connect slice lands (ticket 03); public until then, contract unchanged.
internal sealed class ListMerchantsEndpoint : EndpointBase, IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/merchants", async (
            [AsParameters] ListMerchantsRequest request,
            IListMerchants useCase,
            CancellationToken cancellationToken) =>
        {
            Result<PagedResponse<ListMerchantsResponse>> result = await useCase.ExecuteAsync(request, cancellationToken);
            return CreateResult(result);
        })
        .MapToApiVersion(1, 0)
        .WithTags(Tags.Merchants)
        .Produces<PagedResponse<ListMerchantsResponse>>(StatusCodes.Status200OK)
        .Produces<ApiProblemDetails>(StatusCodes.Status400BadRequest);
    }
}