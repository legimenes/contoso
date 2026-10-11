using API.Middlewares;
using System.Diagnostics;

namespace API.Extensions;

internal static class ErrorHandlingExtensions
{
    internal static IServiceCollection AddErrorHandling(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Title = "One or more errors occurred.";
                context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
                context.ProblemDetails.Extensions["requestId"] = context.HttpContext.TraceIdentifier;
                context.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id;
            };
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    internal static IApplicationBuilder UseErrorHandling(this IApplicationBuilder app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        return app;
    }
}