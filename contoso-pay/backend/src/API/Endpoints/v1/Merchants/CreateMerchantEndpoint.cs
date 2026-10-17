using Core.UseCases.Merchants.Create;
using Domain.Core.Results;

namespace API.Endpoints.v1.Merchants;

internal sealed class CreateMerchantEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder v1 = app.MapVersionedGroup(1);

        v1.MapPost("/merchants", async (CreateMerchantRequest request, ICreateMerchant useCase, CancellationToken cancellationToken) =>
        {
            Result<Guid> result = await useCase.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
            return result.IsSuccess
                ? Results.Created($"/v1/merchants/{result.Value}", new CreateMerchantHttpResponse(result.Value))
                : EndpointBase.CreateResult(result);
        })
        .MapToApiVersion(1, 0)
        .WithTags(Tags.Merchants)
        .Produces<CreateMerchantHttpResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
