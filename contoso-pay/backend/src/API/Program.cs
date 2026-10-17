using API.Extensions;
using Adapters.Persistence;
using Core.UseCases.Merchants.Create;
using FluentValidation;
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
    builder.Services.AddScoped<ICreateMerchant, CreateMerchant>();
    builder.Services.AddScoped<IValidator<CreateMerchantRequest>, CreateMerchantRequestValidator>();

    string postgresConnection = builder.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
    builder.Services.AddPersistence(postgresConnection);
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

public partial class Program;