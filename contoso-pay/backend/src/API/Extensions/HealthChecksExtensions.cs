using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;

namespace API.Extensions;

internal static class HealthChecksExtensions
{
    internal static IServiceCollection AddHealthMonitoring(this IServiceCollection services)
    {
        services.AddHealthChecks();
        //.AddSqlServer(builder.Configuration["ConnectionStrings:Postgres"]!, name: "PostgreSQL");
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
                    //apiVersion
                    dotnetVersion = Environment.Version.ToString(),
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString()
                    })
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        };

        app.MapHealthChecks("/health", healthCheckOptions);
        return app;
    }
}