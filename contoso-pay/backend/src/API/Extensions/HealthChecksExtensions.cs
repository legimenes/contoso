using API.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using MongoDB.Driver;
using System.Text.Json;

namespace API.Extensions;

internal static class HealthChecksExtensions
{
    internal static IServiceCollection AddHealthMonitoring(this IServiceCollection services, ConnectionStrings connectionStrings)
    {
        services.AddHealthChecks()
            .AddNpgSql(connectionStrings.Postgres, name: "PostgreSQL")
            .AddMongoDb(_ => new MongoClient(connectionStrings.MongoDb), name: "MongoDb");
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