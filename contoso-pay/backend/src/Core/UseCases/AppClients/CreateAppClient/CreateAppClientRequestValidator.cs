using Domain.Core.Extensions;
using Domain.Resources;
using FluentValidation;

namespace Core.UseCases.AppClients.CreateAppClient;

public sealed class CreateAppClientRequestValidator : AbstractValidator<CreateAppClientRequest>
{
    public const int NameMaxLength = 100;

    public CreateAppClientRequestValidator()
    {
        RuleFor(static request => request.MerchantId)
            .NotEmpty().WithMessage(Resource.Validation_MerchantId_Required);

        RuleFor(static request => request.Name)
            .NotEmpty().WithMessage(Resource.Validation_Name_Required)
            .MaximumLength(NameMaxLength).WithMessage(Resource.Validation_Name_MaxLength.Format(NameMaxLength));
    }
}
