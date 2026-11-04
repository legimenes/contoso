using Core.UseCases.AppClients.CreateAppClient;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Tests.Core.UseCases.AppClients.CreateAppClient;

public sealed class CreateAppClientEndpointTests(TestingWebApplicationFactory factory) : IClassFixture<TestingWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private readonly string _connectionString = TestingWebApplicationFactory.GetConnectionString(factory);

    [Fact]
    [Trait("AppClient", "CreateAppClient (Integration)")]
    public async Task GivenValidPayload_WhenPostingAppClient_ShouldReturn201WithOpaqueCredential()
    {
        // Given
        Guid merchantId = await CreateMerchantAsync();
        try
        {
            CreateAppClientRequest payload = new CreateAppClientRequestBuilder().WithMerchantId(merchantId).Build();

            // When
            HttpResponseMessage response = await _client.PostAsJsonAsync("/v1/appclients", payload, CancellationToken.None);

            // Then
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(response);
            string clientId = body.RootElement.GetProperty("clientId").GetString()!;
            string clientSecret = body.RootElement.GetProperty("clientSecret").GetString()!;
            Assert.Equal(32, clientId.Length);
            Assert.Equal(43, clientSecret.Length);
            Assert.Equal(clientId, clientId.ToLowerInvariant());
        }
        finally
        {
            await DeleteMerchantAsync(merchantId);
        }
    }

    [Fact]
    [Trait("AppClient", "CreateAppClient (Integration)")]
    public async Task GivenMerchantAlreadyHavingAppClient_WhenPostingAppClient_ShouldReturn409ProblemEnvelope()
    {
        // Given
        Guid merchantId = await CreateMerchantAsync();
        try
        {
            CreateAppClientRequest payload = new CreateAppClientRequestBuilder().WithMerchantId(merchantId).Build();

            // When
            HttpResponseMessage first = await _client.PostAsJsonAsync("/v1/appclients", payload, CancellationToken.None);
            HttpResponseMessage second = await _client.PostAsJsonAsync("/v1/appclients", payload, CancellationToken.None);

            // Then
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
            using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(second);
            AssertProblemEnvelope(body, 409, "Conflict");
        }
        finally
        {
            await DeleteMerchantAsync(merchantId);
        }
    }

    [Fact]
    [Trait("AppClient", "CreateAppClient (Integration)")]
    public async Task GivenUnknownMerchant_WhenPostingAppClient_ShouldReturn404ProblemEnvelope()
    {
        // Given
        CreateAppClientRequest payload = new CreateAppClientRequestBuilder().WithMerchantId(Guid.NewGuid()).Build();

        // When
        HttpResponseMessage response = await _client.PostAsJsonAsync("/v1/appclients", payload, CancellationToken.None);

        // Then
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(response);
        AssertProblemEnvelope(body, 404, "NotFound");
    }

    [Fact]
    [Trait("AppClient", "CreateAppClient (Integration)")]
    public async Task GivenInactiveMerchant_WhenPostingAppClient_ShouldReturn409ProblemEnvelope()
    {
        // Given
        Guid merchantId = await CreateMerchantAsync(isActive: false);
        try
        {
            CreateAppClientRequest payload = new CreateAppClientRequestBuilder().WithMerchantId(merchantId).Build();

            // When
            HttpResponseMessage response = await _client.PostAsJsonAsync("/v1/appclients", payload, CancellationToken.None);

            // Then
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(response);
            AssertProblemEnvelope(body, 409, "Conflict");
        }
        finally
        {
            await DeleteMerchantAsync(merchantId);
        }
    }

    [Fact]
    [Trait("AppClient", "CreateAppClient (Integration)")]
    public async Task GivenBlankName_WhenPostingAppClient_ShouldReturn400ProblemEnvelope()
    {
        // Given
        CreateAppClientRequest payload = new CreateAppClientRequestBuilder().WithName("   ").Build();

        // When
        HttpResponseMessage response = await _client.PostAsJsonAsync("/v1/appclients", payload, CancellationToken.None);

        // Then
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(response);
        AssertProblemEnvelope(body, 400, "BadRequest");
    }

    private static void AssertProblemEnvelope(JsonDocument body, int expectedStatus, string expectedType)
    {
        Assert.Equal(expectedStatus, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(expectedType, body.RootElement.GetProperty("type").GetString());
        Assert.True(body.RootElement.GetProperty("errors").GetArrayLength() > 0);
    }

    private async Task<Guid> CreateMerchantAsync(bool isActive = true)
    {
        return await AppClientEndpointSupport.CreateMerchantAsync(factory, isActive);
    }

    private async Task DeleteMerchantAsync(Guid merchantId)
    {
        await AppClientEndpointSupport.DeleteMerchantAsync(_connectionString, merchantId);
    }
}
