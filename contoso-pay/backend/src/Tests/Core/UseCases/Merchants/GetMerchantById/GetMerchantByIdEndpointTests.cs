using Npgsql;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Tests.Core.UseCases.Merchants.CreateMerchant;

namespace Tests.Core.UseCases.Merchants.GetMerchantById;

public sealed class GetMerchantByIdEndpointTests(TestingWebApplicationFactory factory) : IClassFixture<TestingWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private readonly string _connectionString = TestingWebApplicationFactory.GetConnectionString(factory);

    [Fact]
    [Trait("Merchant", "GetMerchantById (Integration)")]
    public async Task GivenExistingMerchant_WhenGettingMerchantById_ShouldReturn200WithIdNameAndDocument()
    {
        // Given
        string document = NewDocument();
        var payload = new CreateMerchantRequestBuilder().WithDocument(document).Build();
        try
        {
            HttpResponseMessage created = await _client.PostAsJsonAsync("/v1/merchants", payload, CancellationToken.None);
            using JsonDocument createdBody = await TestingWebApplicationFactory.ReadBodyAsync(created);
            Guid id = createdBody.RootElement.GetProperty("id").GetGuid();

            // When
            HttpResponseMessage response = await _client.GetAsync($"/v1/merchants/{id}", CancellationToken.None);

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(response);
            Assert.Equal(id, body.RootElement.GetProperty("id").GetGuid());
            Assert.Equal(payload.Name, body.RootElement.GetProperty("name").GetString());
            Assert.Equal(document, body.RootElement.GetProperty("document").GetString());
        }
        finally
        {
            await DeleteByDocumentAsync(document);
        }
    }

    [Fact]
    [Trait("Merchant", "GetMerchantById (Integration)")]
    public async Task GivenUnknownId_WhenGettingMerchant_ShouldReturn404ProblemEnvelope()
    {
        // Given
        Guid unknownId = Guid.NewGuid();

        // When
        HttpResponseMessage response = await _client.GetAsync($"/v1/merchants/{unknownId}", CancellationToken.None);

        // Then
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(response);
        Assert.Equal(404, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("NotFound", body.RootElement.GetProperty("type").GetString());
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