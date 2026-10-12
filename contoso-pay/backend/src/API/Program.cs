using API.Extensions;
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
}

WebApplication app = builder.Build();
{
    app.UseGlobalization();
    app.UseErrorHandling();
    app.MapEndpoints();
    app.UseDocumentation();
    app.UseHttpsRedirection();

    app.Lifetime.ApplicationStarted.Register(() =>
    {
        Log.Information("Application started and server is ready.");
    });

    app.Run();
}