using Core.Abstractions.Identity;
using Core.UseCases.AppClients.RotateAppClientSecret;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Tests.Core.UseCases.AppClients.RotateAppClientSecret;

public sealed class RotateAppClientSecretEndpointTests(TestingWebApplicationFactory factory) : IClassFixture<TestingWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost")
    });

    private readonly string _connectionString = TestingWebApplicationFactory.GetConnectionString(factory);

    [Fact]
    [Trait("AppClient", "RotateAppClientSecret (Integration)")]
    public async Task GivenCorrectCurrentSecret_WhenRotatingSecret_ShouldReturn200AndNewSecretAuthenticatesWhileOldDoesNot()
    {
        // Given
        Guid merchantId = await CreateMerchantAsync();
        try
        {
            CreatedAppClientCredential appClient = await CreateAppClientAsync(merchantId);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync(appClient, appClient.ClientSecret));

            // When
            HttpResponseMessage response = await PostRotateAsync(appClient.ClientId, appClient.ClientSecret);

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(response);
            string clientId = body.RootElement.GetProperty("clientId").GetString()!;
            string newSecret = body.RootElement.GetProperty("clientSecret").GetString()!;
            Assert.Equal(appClient.ClientId, clientId);
            Assert.NotEqual(appClient.ClientSecret, newSecret);

            HttpResponseMessage tokenWithNewSecret = await PostTokenAsync(appClient.ClientId, newSecret);
            Assert.Equal(HttpStatusCode.OK, tokenWithNewSecret.StatusCode);
            HttpResponseMessage tokenWithOldSecret = await PostTokenAsync(appClient.ClientId, appClient.ClientSecret);
            Assert.Equal(HttpStatusCode.Unauthorized, tokenWithOldSecret.StatusCode);
            using JsonDocument oauthError = await TestingWebApplicationFactory.ReadBodyAsync(tokenWithOldSecret);
            Assert.Equal("invalid_client", oauthError.RootElement.GetProperty("error").GetString());
        }
        finally
        {
            await DeleteMerchantAsync(merchantId);
        }
    }

    [Fact]
    [Trait("AppClient", "RotateAppClientSecret (Integration)")]
    public async Task GivenWrongCurrentSecret_WhenRotatingSecret_ShouldReturn401ProblemEnvelope()
    {
        // Given
        Guid merchantId = await CreateMerchantAsync();
        try
        {
            CreatedAppClientCredential appClient = await CreateAppClientAsync(merchantId);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync(appClient, appClient.ClientSecret));

            // When
            HttpResponseMessage response = await PostRotateAsync(appClient.ClientId, "wrong-secret");

            // Then
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(response);
            Assert.Equal(401, body.RootElement.GetProperty("status").GetInt32());
            Assert.True(body.RootElement.GetProperty("errors").GetArrayLength() > 0);
        }
        finally
        {
            await DeleteMerchantAsync(merchantId);
        }
    }

    [Fact]
    [Trait("AppClient", "RotateAppClientSecret (Integration)")]
    public async Task GivenUnknownOrForeignClient_WhenRotatingSecret_ShouldReturn404()
    {
        // Given
        Guid ownerMerchantId = await CreateMerchantAsync();
        Guid intruderMerchantId = await CreateMerchantAsync();
        try
        {
            CreatedAppClientCredential ownedAppClient = await CreateAppClientAsync(ownerMerchantId);
            CreatedAppClientCredential intruderAppClient = await CreateAppClientAsync(intruderMerchantId);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync(intruderAppClient, intruderAppClient.ClientSecret));

            // When
            HttpResponseMessage response = await PostRotateAsync(ownedAppClient.ClientId, ownedAppClient.ClientSecret);

            // Then
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        finally
        {
            await DeleteMerchantAsync(ownerMerchantId);
            await DeleteMerchantAsync(intruderMerchantId);
        }
    }

    private async Task<HttpResponseMessage> PostRotateAsync(string clientId, string currentSecret)
    {
        return await _client.PostAsJsonAsync(
            $"/v1/appclients/{clientId}/rotate-secret",
            new RotateAppClientSecretRequest(clientId, Guid.Empty, currentSecret),
            CancellationToken.None);
    }

    private async Task<HttpResponseMessage> PostTokenAsync(string clientId, string clientSecret)
    {
        using FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        });
        return await _client.PostAsync("/connect/token", form, CancellationToken.None);
    }

    private async Task<string> GetAccessTokenAsync(CreatedAppClientCredential appClient, string currentSecret)
    {
        HttpResponseMessage tokenResponse = await PostTokenAsync(appClient.ClientId, currentSecret);
        tokenResponse.EnsureSuccessStatusCode();
        using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(tokenResponse);
        return body.RootElement.GetProperty("access_token").GetString()!;
    }

    private async Task<Guid> CreateMerchantAsync(bool isActive = true)
    {
        return await AppClientEndpointSupport.CreateMerchantAsync(factory, isActive);
    }

    private async Task<CreatedAppClientCredential> CreateAppClientAsync(Guid merchantId)
    {
        return await AppClientEndpointSupport.CreateAppClientAsync(factory, merchantId);
    }

    private async Task DeleteMerchantAsync(Guid merchantId)
    {
        await AppClientEndpointSupport.DeleteMerchantAsync(_connectionString, merchantId);
    }
}
