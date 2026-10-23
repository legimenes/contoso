using API.Middlewares;
using Core.UseCases.Merchants.GetMerchantById;
using Domain.Core.Results;

namespace API.Endpoints.v1.Merchants;

// Debt: protected by Bearer when the connect slice lands (ticket 02); public until then, contract unchanged.
internal sealed class GetMerchantByIdEndpoint : EndpointBase, IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/merchants/{id:guid}", async (
            Guid id,
            IGetMerchantById useCase,
            CancellationToken cancellationToken) =>
        {
            Result<GetMerchantByIdResponse> result = await useCase.ExecuteAsync(new GetMerchantByIdRequest(id), cancellationToken);
            return CreateResult(result);
        })
        .MapToApiVersion(1, 0)
        .WithTags(Tags.Merchants)
        .Produces<GetMerchantByIdResponse>(StatusCodes.Status200OK)
        .Produces<ApiProblemDetails>(StatusCodes.Status404NotFound);
    }
}