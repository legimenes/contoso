using Domain;
using Domain.Core.Exceptions;
using Domain.Core.Results;
using Domain.Resources;
using FluentValidation;
using FluentValidation.Results;

namespace Core.UseCases.Merchants.Create;

public sealed class CreateMerchant(
    IValidator<CreateMerchantRequest> validator,
    IMerchantRepository repository) : ICreateMerchant
{
    public async Task<Result<CreateMerchantResponse>> ExecuteAsync(CreateMerchantRequest request, CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<CreateMerchantResponse>(validationResult.ToDictionary(), ResultType.BadRequest);
        }

        string document = request.Document.Trim();
        if (await repository.ExistsByDocumentAsync(document, cancellationToken))
        {
            return Result.Failure<CreateMerchantResponse>(Error.New(Resource.Validation_Document_AlreadyExists), ResultType.Conflict);
        }

        Merchant merchant = Merchant.New(
            name: request.Name,
            document: document);
        try
        {
            await repository.AddAsync(merchant, cancellationToken);
        }
        catch (DuplicateMerchantDocumentException)
        {
            return Result.Failure<CreateMerchantResponse>(Error.New(Resource.Validation_Document_AlreadyExists), ResultType.Conflict);
        }

        return Result.Success<CreateMerchantResponse>(new CreateMerchantResponse(merchant.Id), ResultType.Created);
    }
}