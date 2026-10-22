using Adapters.Persistence;
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
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    public static string GetConnectionString(WebApplicationFactory<Program> factory)
    {
        return factory.Services.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringNames.PostgresConnection)
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
    }

    public static async Task<JsonDocument> ReadBodyAsync(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<JsonDocument>(CancellationToken.None)
            ?? throw new InvalidOperationException("Empty response body.");
    }
}