using Serilog;

namespace API.Extensions;

internal static class LoggingExtensions
{
    internal static void AddLogging(this WebApplicationBuilder builder, string connectionString)
    {
        builder.Host.UseSerilog((context, loggerConfiguration) =>
            loggerConfiguration
                .ReadFrom
                .Configuration(context.Configuration)
                .WriteTo
                .MongoDBBson(connectionString, "logs"));
    }
}