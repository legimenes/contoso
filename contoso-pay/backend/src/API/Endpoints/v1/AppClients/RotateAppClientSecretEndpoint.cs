using API.Middlewares;
using Core.UseCases.AppClients.RotateAppClientSecret;
using Domain.Core.Results;
using Domain.Resources;
using System.Security.Claims;

namespace API.Endpoints.v1.AppClients;

internal sealed class RotateAppClientSecretEndpoint : EndpointBase, IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/appclients/{clientId}/rotate-secret", async (
            string clientId,
            ClaimsPrincipal principal,
            RotateAppClientSecretRequest request,
            IRotateAppClientSecret useCase,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetMerchantId(principal, out Guid merchantId))
            {
                return CreateResult(Result.Failure(Error.New(Resource.Validation_Token_InvalidClient), ResultType.Unauthorized));
            }

            Result<RotateAppClientSecretResponse> result =
                await useCase.ExecuteAsync(new RotateAppClientSecretRequest(clientId, merchantId, request.CurrentSecret), cancellationToken);
            return CreateResult(result);
        })
        .RequireAuthorization()
        .MapToApiVersion(1, 0)
        .WithTags(Tags.AppClients)
        .Produces<RotateAppClientSecretResponse>(StatusCodes.Status200OK)
        .Produces<ApiProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ApiProblemDetails>(StatusCodes.Status404NotFound);
    }
}
