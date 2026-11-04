using Core.Abstractions.Identity;
using Domain.Core.Results;
using Domain.Resources;
using FluentValidation;
using FluentValidation.Results;

namespace Core.UseCases.AppClients.RotateAppClientSecret;

public sealed class RotateAppClientSecret(
    IValidator<RotateAppClientSecretRequest> validator,
    IAppClientProvider appClients) : IRotateAppClientSecret
{
    public async Task<Result<RotateAppClientSecretResponse>> ExecuteAsync(RotateAppClientSecretRequest request, CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<RotateAppClientSecretResponse>(validationResult.ToDictionary(), ResultType.BadRequest);
        }

        RotateAppClientSecretRequest normalizedRequest = request with
        {
            ClientId = request.ClientId.Trim()
        };

        AppClientCredential? appClient = await appClients.FindByClientIdAsync(normalizedRequest.ClientId, cancellationToken);
        if (appClient is null || appClient.MerchantId != normalizedRequest.MerchantId)
        {
            return Result.Failure<RotateAppClientSecretResponse>(Error.New(Resource.Validation_AppClient_NotFound), ResultType.NotFound);
        }

        bool secretIsValid = await appClients.ValidateSecretAsync(normalizedRequest.ClientId, normalizedRequest.CurrentSecret, cancellationToken);
        if (!secretIsValid)
        {
            return Result.Failure<RotateAppClientSecretResponse>(Error.New(Resource.Validation_AppClient_CurrentSecret_Invalid), ResultType.Unauthorized);
        }

        CreatedAppClientCredential? credential = await appClients.RotateSecretAsync(normalizedRequest.ClientId, cancellationToken);
        if (credential is null)
        {
            return Result.Failure<RotateAppClientSecretResponse>(Error.New(Resource.Validation_AppClient_NotFound), ResultType.NotFound);
        }

        return Result.Success<RotateAppClientSecretResponse>(new RotateAppClientSecretResponse(credential.ClientId, credential.ClientSecret), ResultType.Ok);
    }
}
