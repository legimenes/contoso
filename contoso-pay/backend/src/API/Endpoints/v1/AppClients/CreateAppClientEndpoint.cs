using API.Middlewares;
using Core.UseCases.AppClients.CreateAppClient;
using Domain.Core.Results;

namespace API.Endpoints.v1.AppClients;

internal sealed class CreateAppClientEndpoint : EndpointBase, IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/appclients", async (
            CreateAppClientRequest request,
            ICreateAppClient useCase,
            CancellationToken cancellationToken) =>
        {
            Result<CreateAppClientResponse> result = await useCase.ExecuteAsync(request, cancellationToken);
            return CreateResult(result, $"/v1/appclients/{result.Value?.ClientId}");
        })
        .MapToApiVersion(1, 0)
        .WithTags(Tags.AppClients)
        .Produces<CreateAppClientResponse>(StatusCodes.Status201Created)
        .Produces<ApiProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ApiProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ApiProblemDetails>(StatusCodes.Status409Conflict);
    }
}
