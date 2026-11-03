using Adapters.Persistence.Queries;
using Adapters.Persistence.Repositories;
using Core.UseCases.Merchants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Adapters.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("The persistence connection string is not configured.");
        }

        services.AddDbContext<ContosoPayDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IMerchantRepository, MerchantRepository>();
        services.AddScoped<IMerchantQueries, MerchantQueries>();
        return services;
    }
}