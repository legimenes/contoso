using API.Extensions;
using Domain.Core.Results;
using Serilog;
using Serilog.Debugging;
using System.Reflection;

SelfLog.Enable(Console.Error);

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
{
    builder.Host.UseSerilog((context, loggerConfiguration) => loggerConfiguration.ReadFrom.Configuration(context.Configuration));
    builder.Services.AddErrorHandling();
    builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());
    builder.Services.AddOpenApi();
}

WebApplication app = builder.Build();
{
    app.UseGlobalization();
    app.UseErrorHandling();
    app.MapEndpoints();
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }
    app.UseHttpsRedirection();

    app.Lifetime.ApplicationStarted.Register(() =>
    {
        Log.Information("Application started and server is ready.");
    });

    app.MapGet("/test", () =>
    {
        int a = 1 / 1;
        throw new InvalidOperationException("Erro de teste.");
        //return Results.Ok(new { message = "OK" });
        return EndpointBase.CreateResult(Result.Failure<bool>(Error.New("blabla"), ResultType.Undefined));
    });

    app.Run();
}