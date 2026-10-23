using Domain.Core.Extensions;
using Domain.Resources;
using FluentValidation;

namespace Core.UseCases.Merchants.CreateMerchant;

public sealed class CreateMerchantRequestValidator : AbstractValidator<CreateMerchantRequest>
{
    public const int DocumentMaxLength = 14;
    public const int NameMaxLength = 100;

    public CreateMerchantRequestValidator()
    {
        RuleFor(static request => request.Name)
            .NotEmpty().WithMessage(Resource.Validation_Name_Required)
            .MaximumLength(NameMaxLength).WithMessage(Resource.Validation_Name_MaxLength.Format(NameMaxLength));

        RuleFor(static request => request.Document)
            .NotEmpty().WithMessage(Resource.Validation_Document_Required)
            .MaximumLength(DocumentMaxLength).WithMessage(Resource.Validation_Document_MaxLength.Format(DocumentMaxLength));
    }
}