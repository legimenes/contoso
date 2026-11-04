using Domain.Core.Results;
using Domain.Resources;
using FluentValidation;
using FluentValidation.Results;

namespace Core.UseCases.AppClients.GetAppClient;

public sealed class GetAppClient(IValidator<GetAppClientRequest> validator, IAppClientQueries queries) : IGetAppClient
{
    public async Task<Result<GetAppClientResponse>> ExecuteAsync(GetAppClientRequest request, CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<GetAppClientResponse>(validationResult.ToDictionary(), ResultType.BadRequest);
        }

        GetAppClientRequest normalizedRequest = request with
        {
            ClientId = request.ClientId.Trim()
        };

        GetAppClientResponse? appClient = await queries.GetByClientIdAsync(normalizedRequest, cancellationToken);
        if (appClient is null)
        {
            return Result.Failure<GetAppClientResponse>(Error.New(Resource.Validation_AppClient_NotFound), ResultType.NotFound);
        }

        return Result.Success<GetAppClientResponse>(appClient, ResultType.Ok);
    }
}
