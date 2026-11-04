using Domain.Core.Extensions;
using Domain.Resources;
using FluentValidation;

namespace Core.UseCases.AppClients.GetAppClient;

public sealed class GetAppClientRequestValidator : AbstractValidator<GetAppClientRequest>
{
    public GetAppClientRequestValidator()
    {
        RuleFor(static request => request.ClientId)
            .NotEmpty().WithMessage(Resource.Validation_ClientId_Required);
    }
}
