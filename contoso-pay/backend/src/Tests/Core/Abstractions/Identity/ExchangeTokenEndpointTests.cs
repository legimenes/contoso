using Adapters.Identity.AppClients;
using Core.UseCases.Merchants;
using Domain;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Buffers.Text;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Tests.Domain;

namespace Tests.Core.Abstractions.Identity;

public sealed class ExchangeTokenEndpointTests(TestingWebApplicationFactory factory) : IClassFixture<TestingWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost")
    });
    //private readonly HttpClient _client = factory.CreateClient();

    private readonly string _connectionString = TestingWebApplicationFactory.GetConnectionString(factory);

    [Fact]
    [Trait("AppClient", "ExchangeToken (Integration)")]
    public async Task GivenValidClientCredentials_WhenPostingToken_ShouldReturnJwtWithSubAndMerchantId()
    {
        _client.Timeout = TimeSpan.FromMinutes(30);

        // Given
        Guid merchantId = await CreateMerchantAsync();
        try
        {
            CreatedAppClientCredential appClient = await CreateAppClientAsync(merchantId);

            // When
            HttpResponseMessage response = await PostTokenAsync(appClient.ClientId, appClient.ClientSecret);

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(response);
            string accessToken = body.RootElement.GetProperty("access_token").GetString()!;
            Assert.Equal("Bearer", body.RootElement.GetProperty("token_type").GetString());
            Assert.Equal(3600, body.RootElement.GetProperty("expires_in").GetInt32());
            Assert.False(string.IsNullOrWhiteSpace(accessToken));

            using JsonDocument payload = DecodeTokenPayload(accessToken);
            using JsonDocument header = DecodeTokenHeader(accessToken);
            Assert.Equal("HS256", header.RootElement.GetProperty("alg").GetString());
            Assert.Equal(appClient.ClientId, payload.RootElement.GetProperty("sub").GetString());
            Assert.Equal(merchantId.ToString(), payload.RootElement.GetProperty("merchant_id").GetString());
            Assert.Equal("contoso-pay-api", payload.RootElement.GetProperty("aud").GetString());
            Assert.Equal("https://contoso-pay/", payload.RootElement.GetProperty("iss").GetString());
            Assert.True(await CountTokensAsync(appClient.ClientId) > 0);
        }
        finally
        {
            await DeleteMerchantAsync(merchantId);
        }
    }

    [Fact]
    [Trait("AppClient", "ExchangeToken (Integration)")]
    public async Task GivenIssuedToken_WhenCallingProtectedProbe_ShouldAcceptToken()
    {
        // Given
        Guid merchantId = await CreateMerchantAsync();
        try
        {
            CreatedAppClientCredential appClient = await CreateAppClientAsync(merchantId);
            string accessToken = await GetAccessTokenAsync(appClient);

            // When
            HttpResponseMessage response = await SendBearerAsync(AuthenticationProbeStartupFilter.Path, accessToken);

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(response);
            Assert.Equal(appClient.ClientId, body.RootElement.GetProperty("sub").GetString());
            Assert.Equal(merchantId.ToString(), body.RootElement.GetProperty("merchantId").GetString());
        }
        finally
        {
            await DeleteMerchantAsync(merchantId);
        }
    }

    [Fact]
    [Trait("AppClient", "ExchangeToken (Integration)")]
    public async Task GivenUnknownClient_WhenPostingToken_ShouldReturn401InvalidClient()
    {
        // Given
        Guid merchantId = await CreateMerchantAsync();
        try
        {
            CreatedAppClientCredential appClient = await CreateAppClientAsync(merchantId);

            // When
            HttpResponseMessage response = await PostTokenAsync(Guid.NewGuid().ToString("N"), appClient.ClientSecret);

            // Then
            await AssertOAuthErrorAsync(response, HttpStatusCode.Unauthorized, "invalid_client");
        }
        finally
        {
            await DeleteMerchantAsync(merchantId);
        }
    }

    [Fact]
    [Trait("AppClient", "ExchangeToken (Integration)")]
    public async Task GivenWrongSecret_WhenPostingToken_ShouldReturn401InvalidClient()
    {
        // Given
        Guid merchantId = await CreateMerchantAsync();
        try
        {
            CreatedAppClientCredential appClient = await CreateAppClientAsync(merchantId);

            // When
            HttpResponseMessage response = await PostTokenAsync(appClient.ClientId, "wrong-secret");

            // Then
            await AssertOAuthErrorAsync(response, HttpStatusCode.Unauthorized, "invalid_client");
        }
        finally
        {
            await DeleteMerchantAsync(merchantId);
        }
    }

    [Fact]
    [Trait("AppClient", "ExchangeToken (Integration)")]
    public async Task GivenInactiveMerchant_WhenPostingToken_ShouldReturn401InvalidClient()
    {
        // Given
        Guid merchantId = await CreateMerchantAsync(isActive: false);
        try
        {
            CreatedAppClientCredential appClient = await CreateAppClientAsync(merchantId);

            // When
            HttpResponseMessage response = await PostTokenAsync(appClient.ClientId, appClient.ClientSecret);

            // Then
            await AssertOAuthErrorAsync(response, HttpStatusCode.Unauthorized, "invalid_client");
        }
        finally
        {
            await DeleteMerchantAsync(merchantId);
        }
    }

    [Fact]
    [Trait("AppClient", "ExchangeToken (Integration)")]
    public async Task GivenUnsupportedGrantType_WhenPostingToken_ShouldReturn400UnsupportedGrantType()
    {
        // Given
        Guid merchantId = await CreateMerchantAsync();
        try
        {
            CreatedAppClientCredential appClient = await CreateAppClientAsync(merchantId);

            // When
            HttpResponseMessage response = await PostTokenAsync(appClient.ClientId, appClient.ClientSecret, "password");

            // Then
            await AssertOAuthErrorAsync(response, HttpStatusCode.BadRequest, "unsupported_grant_type");
        }
        finally
        {
            await DeleteMerchantAsync(merchantId);
        }
    }

    [Fact]
    [Trait("AppClient", "ExchangeToken (Integration)")]
    public async Task GivenMissingClientSecret_WhenPostingToken_ShouldReturn400InvalidRequest()
    {
        // Given
        Guid merchantId = await CreateMerchantAsync();
        try
        {
            CreatedAppClientCredential appClient = await CreateAppClientAsync(merchantId);
            using FormUrlEncodedContent form = new(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = appClient.ClientId
            });

            // When
            HttpResponseMessage response = await _client.PostAsync("/connect/token", form, CancellationToken.None);

            // Then
            await AssertOAuthErrorAsync(response, HttpStatusCode.BadRequest, "invalid_request");
        }
        finally
        {
            await DeleteMerchantAsync(merchantId);
        }
    }

    [Fact]
    [Trait("AppClient", "ExchangeToken (Integration)")]
    public async Task GivenJsonBody_WhenPostingToken_ShouldReturn400InvalidRequest()
    {
        // Given
        using StringContent content = new("""{"grant_type":"client_credentials"}""", Encoding.UTF8, "application/json");

        // When
        HttpResponseMessage response = await _client.PostAsync("/connect/token", content, CancellationToken.None);

        // Then
        await AssertOAuthErrorAsync(response, HttpStatusCode.BadRequest, "invalid_request");
    }

    [Fact]
    [Trait("AppClient", "ExchangeToken (Integration)")]
    public async Task GivenTamperedToken_WhenCallingProtectedProbe_ShouldReturn401()
    {
        // Given
        Guid merchantId = await CreateMerchantAsync();
        try
        {
            CreatedAppClientCredential appClient = await CreateAppClientAsync(merchantId);
            string accessToken = await GetAccessTokenAsync(appClient);
            string[] segments = accessToken.Split('.');
            string tamperedToken = $"{segments[0]}.{segments[1]}.{new string('A', segments[2].Length)}";

            // When
            HttpResponseMessage response = await SendBearerAsync(AuthenticationProbeStartupFilter.Path, tamperedToken);

            // Then
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await DeleteMerchantAsync(merchantId);
        }
    }

    [Fact]
    [Trait("AppClient", "ExchangeToken (Integration)")]
    public async Task GivenExpiredToken_WhenCallingProtectedProbe_ShouldReturn401()
    {
        // Given
        Guid merchantId = await CreateMerchantAsync();
        DateTimeOffset initialTime = factory.TimeProvider.GetUtcNow();
        try
        {
            CreatedAppClientCredential appClient = await CreateAppClientAsync(merchantId);
            string accessToken = await GetAccessTokenAsync(appClient);
            factory.TimeProvider.Advance(TimeSpan.FromMinutes(61));

            // When
            HttpResponseMessage response = await SendBearerAsync(AuthenticationProbeStartupFilter.Path, accessToken);

            // Then
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            factory.TimeProvider.Reset(initialTime);
            await DeleteMerchantAsync(merchantId);
        }
    }

    private async Task<Guid> CreateMerchantAsync(bool isActive = true)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        IMerchantRepository repository = scope.ServiceProvider.GetRequiredService<IMerchantRepository>();
        Merchant merchant = isActive
            ? new MerchantBuilder().Build()
            : new MerchantBuilder().Inactive().Build();
        await repository.AddAsync(merchant, CancellationToken.None);
        return merchant.Id;
    }

    private async Task<CreatedAppClientCredential> CreateAppClientAsync(Guid merchantId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        IAppClientCredentials credentials = scope.ServiceProvider.GetRequiredService<IAppClientCredentials>();
        return await credentials.CreateAsync(merchantId, "Loja Exemplo", CancellationToken.None);
    }

    private async Task<HttpResponseMessage> PostTokenAsync(
        string clientId,
        string clientSecret,
        string grantType = "client_credentials")
    {
        using FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["grant_type"] = grantType,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        });
        return await _client.PostAsync("/connect/token", form, CancellationToken.None);
    }

    private async Task<string> GetAccessTokenAsync(CreatedAppClientCredential appClient)
    {
        HttpResponseMessage response = await PostTokenAsync(appClient.ClientId, appClient.ClientSecret);
        response.EnsureSuccessStatusCode();
        using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(response);
        return body.RootElement.GetProperty("access_token").GetString()!;
    }

    private async Task<HttpResponseMessage> SendBearerAsync(string path, string accessToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request, CancellationToken.None);
    }

    private static JsonDocument DecodeTokenHeader(string accessToken)
    {
        string[] segments = accessToken.Split('.');
        byte[] header = Base64Url.DecodeFromChars(segments[0]);
        return JsonDocument.Parse(header);
    }

    private static JsonDocument DecodeTokenPayload(string accessToken)
    {
        string[] segments = accessToken.Split('.');
        byte[] payload = Base64Url.DecodeFromChars(segments[1]);
        return JsonDocument.Parse(payload);
    }

    private static async Task AssertOAuthErrorAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedError)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(response);
        Assert.Equal(expectedError, body.RootElement.GetProperty("error").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("error_description").GetString()));
    }

    private async Task<long> CountTokensAsync(string clientId)
    {
        await using NpgsqlConnection connection = new(_connectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(
            """
            SELECT COUNT(*)
            FROM contosopay.app_client_tokens AS token
            JOIN contosopay.app_clients AS app_client ON app_client.id = token.application_id
            WHERE app_client.client_id = @clientId
            """,
            connection);
        command.Parameters.AddWithValue("clientId", clientId);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private async Task DeleteMerchantAsync(Guid merchantId)
    {
        await using NpgsqlConnection connection = new(_connectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(
            """
            DELETE FROM contosopay.app_client_tokens
            WHERE application_id IN (SELECT id FROM contosopay.app_clients WHERE merchant_id = @merchantId);

            DELETE FROM contosopay.app_clients WHERE merchant_id = @merchantId;
            DELETE FROM contosopay.merchants WHERE id = @merchantId;
            """,
            connection);
        command.Parameters.AddWithValue("merchantId", merchantId);
        await command.ExecuteNonQueryAsync();
    }
}