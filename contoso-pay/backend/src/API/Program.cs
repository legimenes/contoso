using Adapters.Identity;
using Adapters.Persistence;
using API.Extensions;
using API.Infrastructure;
using Core;
using Serilog;
using Serilog.Debugging;
using System.Reflection;

SelfLog.Enable(Console.Error);

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
{
    ConnectionStrings connectionStrings = new(builder.Configuration);
    connectionStrings.Validate();

    builder.AddLogging(connectionStrings.MongoDb);
    builder.Services.AddErrorHandling();
    builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());
    builder.Services.AddVersioning();
    builder.Services.AddHealthMonitoring(connectionStrings);
    builder.Services.AddCore();
    builder.Services.AddPersistence(connectionStrings.Postgres);
    builder.Services.AddIdentity(connectionStrings.Postgres, options =>
        builder.Configuration.GetSection("Identity").Bind(options));
}

WebApplication app = builder.Build();
{
    app.UseGlobalization();
    app.UseErrorHandling();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapEndpoints();
    app.UseDocumentation();
    app.UseHttpsRedirection();
    app.UseHealthMonitoring();

    app.Lifetime.ApplicationStarted.Register(() =>
    {
        Log.Information("Application started and server is ready.");
    });

    app.Run();
}