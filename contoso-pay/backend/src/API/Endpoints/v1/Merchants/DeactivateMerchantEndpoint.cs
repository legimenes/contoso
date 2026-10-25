using API.Middlewares;
using Core.UseCases.Merchants.DeactivateMerchant;
using Domain.Core.Results;

namespace API.Endpoints.v1.Merchants;

// Debt: protected by Bearer when the connect slice lands; public until then, contract unchanged.
internal sealed class DeactivateMerchantEndpoint : EndpointBase, IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/merchants/{id:guid}/deactivate", async (
            Guid id,
            IDeactivateMerchant useCase,
            CancellationToken cancellationToken) =>
        {
            Result result = await useCase.ExecuteAsync(new DeactivateMerchantRequest(id), cancellationToken);
            return CreateResult(result);
        })
        .MapToApiVersion(1, 0)
        .WithTags(Tags.Merchants)
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiProblemDetails>(StatusCodes.Status404NotFound);
    }
}