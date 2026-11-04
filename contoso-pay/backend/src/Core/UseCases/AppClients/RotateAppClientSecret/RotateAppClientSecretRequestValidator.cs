using Domain.Resources;
using FluentValidation;

namespace Core.UseCases.AppClients.RotateAppClientSecret;

public sealed class RotateAppClientSecretRequestValidator : AbstractValidator<RotateAppClientSecretRequest>
{
    public RotateAppClientSecretRequestValidator()
    {
        RuleFor(static request => request.ClientId)
            .NotEmpty().WithMessage(Resource.Validation_ClientId_Required);

        RuleFor(static request => request.CurrentSecret)
            .NotEmpty().WithMessage(Resource.Validation_AppClient_CurrentSecret_Required);
    }
}
