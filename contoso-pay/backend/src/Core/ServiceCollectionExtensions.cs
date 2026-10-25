using Core.UseCases.Merchants.ActivateMerchant;
using Core.UseCases.Merchants.CreateMerchant;
using Core.UseCases.Merchants.DeactivateMerchant;
using Core.UseCases.Merchants.GetMerchantById;
using Core.UseCases.Merchants.ListMerchants;
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
        services.AddScoped<IDeactivateMerchant, DeactivateMerchant>();
        services.AddScoped<IActivateMerchant, ActivateMerchant>();
        services.AddScoped<IListMerchants, ListMerchants>();
        services.AddScoped<IValidator<ListMerchantsRequest>, ListMerchantsRequestValidator>();
        return services;
    }
}