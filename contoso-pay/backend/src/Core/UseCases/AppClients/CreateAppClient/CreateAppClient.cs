using Core.Abstractions.Identity;
using Core.UseCases.Merchants;
using Domain;
using Domain.Core.Exceptions;
using Domain.Core.Results;
using Domain.Resources;
using FluentValidation;
using FluentValidation.Results;

namespace Core.UseCases.AppClients.CreateAppClient;

public sealed class CreateAppClient(
    IValidator<CreateAppClientRequest> validator,
    IMerchantRepository merchants,
    IAppClientProvider appClients) : ICreateAppClient
{
    public async Task<Result<CreateAppClientResponse>> ExecuteAsync(CreateAppClientRequest request, CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<CreateAppClientResponse>(validationResult.ToDictionary(), ResultType.BadRequest);
        }

        Merchant? merchant = await merchants.GetByIdAsync(request.MerchantId, cancellationToken);
        if (merchant is null)
        {
            return Result.Failure<CreateAppClientResponse>(Error.New(Resource.Validation_Merchant_NotFound), ResultType.NotFound);
        }

        if (!merchant.IsActive)
        {
            return Result.Failure<CreateAppClientResponse>(Error.New(Resource.Validation_Merchant_Inactive), ResultType.Conflict);
        }

        if (await appClients.ExistsByMerchantIdAsync(merchant.Id, cancellationToken))
        {
            return Result.Failure<CreateAppClientResponse>(Error.New(Resource.Validation_AppClient_AlreadyExists), ResultType.Conflict);
        }

        try
        {
            CreatedAppClientCredential credential = await appClients.CreateAsync(merchant.Id, request.Name.Trim(), cancellationToken);
            return Result.Success<CreateAppClientResponse>(new CreateAppClientResponse(credential.ClientId, credential.ClientSecret), ResultType.Created);
        }
        catch (DuplicateAppClientException)
        {
            return Result.Failure<CreateAppClientResponse>(Error.New(Resource.Validation_AppClient_AlreadyExists), ResultType.Conflict);
        }
    }
}
