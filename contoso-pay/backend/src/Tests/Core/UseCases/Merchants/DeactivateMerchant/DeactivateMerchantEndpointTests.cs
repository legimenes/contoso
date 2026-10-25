using Npgsql;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Tests.Core.UseCases.Merchants.CreateMerchant;

namespace Tests.Core.UseCases.Merchants.DeactivateMerchant;

public sealed class DeactivateMerchantEndpointTests(TestingWebApplicationFactory factory) : IClassFixture<TestingWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private readonly string _connectionString = TestingWebApplicationFactory.GetConnectionString(factory);

    [Fact]
    [Trait("Merchant", "DeactivateMerchant (Integration)")]
    public async Task GivenExistingMerchant_WhenDeactivating_ShouldReturn204WithoutBodyAndPersistInactive()
    {
        // Given
        string document = NewDocument();
        var payload = new CreateMerchantRequestBuilder().WithDocument(document).Build();
        try
        {
            HttpResponseMessage created = await _client.PostAsJsonAsync("/v1/merchants", payload, CancellationToken.None);
            using JsonDocument createdBody = await TestingWebApplicationFactory.ReadBodyAsync(created);
            Guid id = createdBody.RootElement.GetProperty("id").GetGuid();
            DateTime createdAt = await GetCreatedAtAsync(document);

            // When
            HttpResponseMessage response = await _client.PostAsync($"/v1/merchants/{id}/deactivate", content: null, CancellationToken.None);

            // Then
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(CancellationToken.None));

            HttpResponseMessage read = await _client.GetAsync($"/v1/merchants/{id}", CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, read.StatusCode);
            using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(read);
            Assert.Equal(id, body.RootElement.GetProperty("id").GetGuid());
            Assert.Equal(payload.Name, body.RootElement.GetProperty("name").GetString());
            Assert.Equal(document, body.RootElement.GetProperty("document").GetString());
            Assert.False(body.RootElement.GetProperty("isActive").GetBoolean());
            Assert.Equal(createdAt, await GetCreatedAtAsync(document));
        }
        finally
        {
            await DeleteByDocumentAsync(document);
        }
    }

    [Fact]
    [Trait("Merchant", "DeactivateMerchant (Integration)")]
    public async Task GivenAlreadyInactiveMerchant_WhenDeactivatingAgain_ShouldReturn204WithoutBody()
    {
        // Given
        string document = NewDocument();
        var payload = new CreateMerchantRequestBuilder().WithDocument(document).Build();
        try
        {
            HttpResponseMessage created = await _client.PostAsJsonAsync("/v1/merchants", payload, CancellationToken.None);
            using JsonDocument createdBody = await TestingWebApplicationFactory.ReadBodyAsync(created);
            Guid id = createdBody.RootElement.GetProperty("id").GetGuid();
            await _client.PostAsync($"/v1/merchants/{id}/deactivate", content: null, CancellationToken.None);

            // When
            HttpResponseMessage repeated = await _client.PostAsync($"/v1/merchants/{id}/deactivate", content: null, CancellationToken.None);

            // Then
            Assert.Equal(HttpStatusCode.NoContent, repeated.StatusCode);
            Assert.Equal(string.Empty, await repeated.Content.ReadAsStringAsync(CancellationToken.None));
        }
        finally
        {
            await DeleteByDocumentAsync(document);
        }
    }

    [Fact]
    [Trait("Merchant", "DeactivateMerchant (Integration)")]
    public async Task GivenUnknownId_WhenDeactivating_ShouldReturn404ProblemEnvelope()
    {
        // Given
        Guid unknownId = Guid.NewGuid();

        // When
        HttpResponseMessage response = await _client.PostAsync($"/v1/merchants/{unknownId}/deactivate", content: null, CancellationToken.None);

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

    private async Task<DateTime> GetCreatedAtAsync(string document)
    {
        await using NpgsqlConnection connection = new(_connectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new("SELECT created_at FROM contosopay.merchants WHERE document = @document", connection);
        command.Parameters.AddWithValue("document", document);
        return (DateTime)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException($"Merchant with document '{document}' was not found."));
    }

    private async Task DeleteByDocumentAsync(string document)
    {
        await using NpgsqlConnection connection = new(_connectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new("DELETE FROM contosopay.merchants WHERE document = @document", connection);
        command.Parameters.AddWithValue("document", document);
        await command.ExecuteNonQueryAsync();
    }
}