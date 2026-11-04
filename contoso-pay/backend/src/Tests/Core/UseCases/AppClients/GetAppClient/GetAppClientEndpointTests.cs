using Core.Abstractions.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Tests.Core.UseCases.AppClients.GetAppClient;

public sealed class GetAppClientEndpointTests(TestingWebApplicationFactory factory) : IClassFixture<TestingWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost")
    });

    private readonly string _connectionString = TestingWebApplicationFactory.GetConnectionString(factory);

    [Fact]
    [Trait("AppClient", "GetAppClient (Integration)")]
    public async Task GivenOwnedAppClient_WhenGettingAppClientWithBearerToken_ShouldReturn200WithoutSecret()
    {
        // Given
        Guid merchantId = await CreateMerchantAsync();
        try
        {
            CreatedAppClientCredential appClient = await CreateAppClientAsync(merchantId);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync(appClient));

            // When
            HttpResponseMessage response = await _client.GetAsync($"/v1/appclients/{appClient.ClientId}", CancellationToken.None);

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(response);
            string clientId = body.RootElement.GetProperty("clientId").GetString()!;
            Assert.Equal(appClient.ClientId, clientId);
            Assert.Equal(merchantId.ToString(), body.RootElement.GetProperty("merchantId").GetString());
            Assert.Equal("Loja Exemplo", body.RootElement.GetProperty("name").GetString());
            Assert.False(body.RootElement.TryGetProperty("clientSecret", out _));
        }
        finally
        {
            await DeleteMerchantAsync(merchantId);
        }
    }

    [Fact]
    [Trait("AppClient", "GetAppClient (Integration)")]
    public async Task GivenNoBearerToken_WhenGettingAppClient_ShouldReturn401()
    {
        // Given
        Guid merchantId = await CreateMerchantAsync();
        try
        {
            CreatedAppClientCredential appClient = await CreateAppClientAsync(merchantId);

            // When
            HttpResponseMessage response = await _client.GetAsync($"/v1/appclients/{appClient.ClientId}", CancellationToken.None);

            // Then
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await DeleteMerchantAsync(merchantId);
        }
    }

    [Fact]
    [Trait("AppClient", "GetAppClient (Integration)")]
    public async Task GivenOtherMerchantToken_WhenGettingAppClientOfAnotherMerchant_ShouldReturn404()
    {
        // Given
        Guid ownerMerchantId = await CreateMerchantAsync();
        Guid intruderMerchantId = await CreateMerchantAsync();
        try
        {
            CreatedAppClientCredential ownedAppClient = await CreateAppClientAsync(ownerMerchantId);
            CreatedAppClientCredential intruderAppClient = await CreateAppClientAsync(intruderMerchantId);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync(intruderAppClient));

            // When
            HttpResponseMessage response = await _client.GetAsync($"/v1/appclients/{ownedAppClient.ClientId}", CancellationToken.None);

            // Then
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        finally
        {
            await DeleteMerchantAsync(ownerMerchantId);
            await DeleteMerchantAsync(intruderMerchantId);
        }
    }

    [Fact]
    [Trait("AppClient", "GetAppClient (Integration)")]
    public async Task GivenUnknownClient_WhenGettingAppClientWithBearerToken_ShouldReturn404()
    {
        // Given
        Guid merchantId = await CreateMerchantAsync();
        try
        {
            CreatedAppClientCredential appClient = await CreateAppClientAsync(merchantId);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync(appClient));

            // When
            HttpResponseMessage response = await _client.GetAsync($"/v1/appclients/{Guid.NewGuid().ToString("N")}", CancellationToken.None);

            // Then
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        finally
        {
            await DeleteMerchantAsync(merchantId);
        }
    }

    private async Task<Guid> CreateMerchantAsync(bool isActive = true)
    {
        return await AppClientEndpointSupport.CreateMerchantAsync(factory, isActive);
    }

    private async Task<CreatedAppClientCredential> CreateAppClientAsync(Guid merchantId)
    {
        return await AppClientEndpointSupport.CreateAppClientAsync(factory, merchantId);
    }

    private async Task<string> GetAccessTokenAsync(CreatedAppClientCredential appClient)
    {
        using FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = appClient.ClientId,
            ["client_secret"] = appClient.ClientSecret
        });
        HttpResponseMessage response = await _client.PostAsync("/connect/token", form, CancellationToken.None);
        response.EnsureSuccessStatusCode();
        using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(response);
        return body.RootElement.GetProperty("access_token").GetString()!;
    }

    private async Task DeleteMerchantAsync(Guid merchantId)
    {
        await AppClientEndpointSupport.DeleteMerchantAsync(_connectionString, merchantId);
    }
}
