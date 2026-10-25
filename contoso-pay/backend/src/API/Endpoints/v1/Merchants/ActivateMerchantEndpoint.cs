using API.Middlewares;
using Core.UseCases.Merchants.ActivateMerchant;
using Domain.Core.Results;

namespace API.Endpoints.v1.Merchants;

// Debt: protected by Bearer when the connect slice lands; public until then, contract unchanged.
internal sealed class ActivateMerchantEndpoint : EndpointBase, IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/merchants/{id:guid}/activate", async (
            Guid id,
            IActivateMerchant useCase,
            CancellationToken cancellationToken) =>
        {
            Result result = await useCase.ExecuteAsync(new ActivateMerchantRequest(id), cancellationToken);
            return CreateResult(result);
        })
        .MapToApiVersion(1, 0)
        .WithTags(Tags.Merchants)
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiProblemDetails>(StatusCodes.Status404NotFound);
    }
}