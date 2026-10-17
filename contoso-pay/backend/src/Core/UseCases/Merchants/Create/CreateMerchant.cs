using Domain;
using Domain.Core.Exceptions;
using Domain.Core.Results;
using Domain.Resources;
using FluentValidation;
using FluentValidation.Results;

namespace Core.UseCases.Merchants.Create;

public sealed class CreateMerchant(IMerchantRepository repository, IValidator<CreateMerchantRequest> validator) : ICreateMerchant
{
    public async Task<Result<Guid>> ExecuteAsync(CreateMerchantRequest request, CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<Guid>(validationResult.ToDictionary(), ResultType.BadRequest);
        }

        string document = request.Document.Trim();
        if (await repository.ExistsByDocumentAsync(document, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<Guid>(Error.New(Resource.Validation_Document_AlreadyExists), ResultType.Conflict);
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
            return Result.Failure<Guid>(Error.New(Resource.Validation_Document_AlreadyExists), ResultType.Conflict);
        }

        return Result.Success<Guid>(merchant.Id, ResultType.Created);
    }
}