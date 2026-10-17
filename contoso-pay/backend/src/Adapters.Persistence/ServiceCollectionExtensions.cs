using Adapters.Persistence.Repositories;
using Core.UseCases.Merchants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Adapters.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ContosoPayDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IMerchantRepository, MerchantRepository>();
        return services;
    }
}