using Adapters.Persistence.Repositories;
using Core.UseCases.Merchants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Adapters.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        string postgresConnectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
        services.AddDbContext<ContosoPayDbContext>(options => options.UseNpgsql(postgresConnectionString));
        services.AddScoped<IMerchantRepository, MerchantRepository>();
        return services;
    }
}