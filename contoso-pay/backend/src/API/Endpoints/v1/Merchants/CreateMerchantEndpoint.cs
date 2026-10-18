using API.Middlewares;
using Core.UseCases.Merchants.Create;
using Domain.Core.Results;

namespace API.Endpoints.v1.Merchants;

internal sealed class CreateMerchantEndpoint : EndpointBase, IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder v1 = app.MapVersionedGroup(1);

        v1.MapPost("/merchants", async (
            CreateMerchantRequest request,
            ICreateMerchant useCase,
            CancellationToken cancellationToken) =>
        {
            Result<CreateMerchantResponse> result = await useCase.ExecuteAsync(request, cancellationToken);
            return CreateResult(result, $"/v1/merchants/{result.Value?.Id}");
        })
        .MapToApiVersion(1, 0)
        .WithTags(Tags.Merchants)
        .Produces<CreateMerchantResponse>(StatusCodes.Status201Created)
        .Produces<ApiProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ApiProblemDetails>(StatusCodes.Status409Conflict);
    }
}