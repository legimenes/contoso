using Adapters.Persistence;
using API.Extensions;
using Core;
using Serilog;
using Serilog.Debugging;
using System.Reflection;

SelfLog.Enable(Console.Error);

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
{
    builder.Host.UseSerilog((context, loggerConfiguration) => loggerConfiguration.ReadFrom.Configuration(context.Configuration));
    builder.Services.AddErrorHandling();
    builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());
    builder.Services.AddVersioning();
    builder.Services.AddHealthMonitoring();
    builder.Services.AddCore();
    builder.Services.AddPersistence(builder.Configuration);
}

WebApplication app = builder.Build();
{
    app.UseGlobalization();
    app.UseErrorHandling();
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