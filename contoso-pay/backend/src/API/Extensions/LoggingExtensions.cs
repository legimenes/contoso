using API.Infrastructure;
using Serilog;

namespace API.Extensions;

internal static class LoggingExtensions
{
    internal static void AddLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, loggerConfiguration) => 
            loggerConfiguration
                .ReadFrom
                .Configuration(context.Configuration)
                .WriteTo
                .MongoDBBson(builder.Configuration.GetConnectionString(MongoDBConnectionString.MongoDbConnection)!, "logs"));
    }
}