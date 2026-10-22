using Core.UseCases.Merchants.Create;
using Core.UseCases.Merchants.GetById;
using Core.UseCases.Merchants.List;
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
        services.AddScoped<IListMerchants, ListMerchants>();
        services.AddScoped<IValidator<ListMerchantsRequest>, ListMerchantsRequestValidator>();
        return services;
    }
}