using Core.UseCases.Merchants.CreateMerchant;
using Npgsql;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Tests.Core.UseCases.Merchants.CreateMerchant;

public sealed class CreateMerchantEndpointTests(TestingWebApplicationFactory factory) : IClassFixture<TestingWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private readonly string _connectionString = TestingWebApplicationFactory.GetConnectionString(factory);

    [Fact]
    [Trait("Merchant", "CreateMerchant (Integration)")]
    public async Task GivenValidPayload_WhenPostingMerchant_ShouldReturn201WithOnlyId()
    {
        // Given
        string document = NewDocument();
        try
        {
            CreateMerchantRequest payload = new CreateMerchantRequestBuilder().WithDocument(document).Build();

            // When
            HttpResponseMessage response = await _client.PostAsJsonAsync("/v1/merchants", payload, CancellationToken.None);

            // Then
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(response);
            Assert.True(body.RootElement.TryGetProperty("id", out JsonElement id));
            Assert.NotEqual(Guid.Empty, id.GetGuid());
        }
        finally
        {
            await DeleteByDocumentAsync(document);
        }
    }

    [Fact]
    [Trait("Merchant", "CreateMerchant (Integration)")]
    public async Task GivenMissingDocument_WhenPostingMerchant_ShouldReturn400ProblemEnvelope()
    {
        // Given
        var payload = new CreateMerchantRequestBuilder().WithDocument(string.Empty).Build();

        // When
        HttpResponseMessage response = await _client.PostAsJsonAsync("/v1/merchants", payload, CancellationToken.None);

        // Then
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(response);
        AssertProblemEnvelope(body, 400, "BadRequest");
    }

    [Fact]
    [Trait("Merchant", "CreateMerchant (Integration)")]
    public async Task GivenDuplicateDocument_WhenPostingTwice_ShouldReturn201Then409ProblemEnvelope()
    {
        // Given
        string document = NewDocument();
        try
        {
            var firstPayload = new CreateMerchantRequestBuilder().WithName("Loja Um").WithDocument(document).Build();
            var secondPayload = new CreateMerchantRequestBuilder().WithName("Loja Dois").WithDocument(document).Build();

            // When
            HttpResponseMessage first = await _client.PostAsJsonAsync("/v1/merchants", firstPayload, CancellationToken.None);
            HttpResponseMessage second = await _client.PostAsJsonAsync("/v1/merchants", secondPayload, CancellationToken.None);

            // Then
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
            using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(second);
            AssertProblemEnvelope(body, 409, "Conflict");
        }
        finally
        {
            await DeleteByDocumentAsync(document);
        }
    }

    private static void AssertProblemEnvelope(JsonDocument body, int expectedStatus, string expectedType)
    {
        Assert.Equal(expectedStatus, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(expectedType, body.RootElement.GetProperty("type").GetString());
        Assert.True(body.RootElement.GetProperty("errors").GetArrayLength() > 0);
    }

    private static string NewDocument()
    {
        return Random.Shared.NextInt64(10_000_000_000L, 100_000_000_000_000L).ToString();
    }

    private async Task DeleteByDocumentAsync(string document)
    {
        await using NpgsqlConnection connection = new (_connectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new ("DELETE FROM contosopay.merchants WHERE document = @document", connection);
        command.Parameters.AddWithValue("document", document);
        await command.ExecuteNonQueryAsync();
    }
}