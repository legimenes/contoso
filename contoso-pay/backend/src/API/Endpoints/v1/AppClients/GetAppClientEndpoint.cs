using API.Middlewares;
using Core.UseCases.AppClients.GetAppClient;
using Domain.Core.Results;
using Domain.Resources;
using System.Security.Claims;

namespace API.Endpoints.v1.AppClients;

internal sealed class GetAppClientEndpoint : EndpointBase, IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/appclients/{clientId}", async (
            string clientId,
            ClaimsPrincipal principal,
            IGetAppClient useCase,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetMerchantId(principal, out Guid merchantId))
            {
                return CreateResult(Result.Failure(Error.New(Resource.Validation_Token_InvalidClient), ResultType.Unauthorized));
            }

            Result<GetAppClientResponse> result = await useCase.ExecuteAsync(new GetAppClientRequest(clientId, merchantId), cancellationToken);
            return CreateResult(result);
        })
        .RequireAuthorization()
        .MapToApiVersion(1, 0)
        .WithTags(Tags.AppClients)
        .Produces<GetAppClientResponse>(StatusCodes.Status200OK)
        .Produces<ApiProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ApiProblemDetails>(StatusCodes.Status404NotFound);
    }
}
