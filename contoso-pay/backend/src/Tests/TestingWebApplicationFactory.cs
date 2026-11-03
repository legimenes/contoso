using API.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;

namespace Tests;

public sealed class TestingWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private readonly TestTimeProvider _timeProvider = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    public TestTimeProvider TimeProvider
    {
        get
        {
            return _timeProvider;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<TimeProvider>(_timeProvider);
            services.AddSingleton<IStartupFilter, AuthenticationProbeStartupFilter>();
        });
    }

    public static string GetConnectionString(WebApplicationFactory<Program> factory)
    {
        return factory.Services.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStrings.PostgresName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStrings.PostgresName}' is not configured.");
    }

    public static async Task<JsonDocument> ReadBodyAsync(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<JsonDocument>(CancellationToken.None)
            ?? throw new InvalidOperationException("Empty response body.");
    }
}