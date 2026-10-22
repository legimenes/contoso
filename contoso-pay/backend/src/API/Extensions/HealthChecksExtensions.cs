using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;

namespace API.Extensions;

internal static class HealthChecksExtensions
{
    internal static IServiceCollection AddHealthMonitoring(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("Postgres")!, name: "PostgreSQL");
        return services;
    }

    internal static IApplicationBuilder UseHealthMonitoring(this WebApplication app)
    {
        HealthCheckOptions healthCheckOptions = new()
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";

                var response = new
                {
                    ApplicationName = ApplicationInfo.Name,
                    ApplicationVersion = ApplicationInfo.GetApplicationVersion(),
                    DotnetVersion = ApplicationInfo.GetNETVersion(),
                    Status = report.Status.ToString(),
                    Checks = report.Entries.Select(entry => new
                    {
                        Name = entry.Key,
                        Status = entry.Value.Status.ToString()
                    })
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        };

        app.MapHealthChecks("/health", healthCheckOptions);
        return app;
    }
}