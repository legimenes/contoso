using Core.UseCases.Merchants.Create;
using Core.UseCases.Merchants.GetById;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddScoped<ICreateMerchant, CreateMerchant>();
        services.AddScoped<IValidator<CreateMerchantRequest>, CreateMerchantRequestValidator>();
        services.AddScoped<IGetMerchantById, GetMerchantById>();
        return services;
    }
}