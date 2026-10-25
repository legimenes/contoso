using Npgsql;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Tests.Core.UseCases.Merchants.CreateMerchant;

namespace Tests.Core.UseCases.Merchants.ActivateMerchant;

public sealed class ActivateMerchantEndpointTests(TestingWebApplicationFactory factory) : IClassFixture<TestingWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private readonly string _connectionString = TestingWebApplicationFactory.GetConnectionString(factory);

    [Fact]
    [Trait("Merchant", "ActivateMerchant (Integration)")]
    public async Task GivenInactiveMerchant_WhenActivating_ShouldReturn204WithoutBodyAndPersistActive()
    {
        // Given
        string document = NewDocument();
        var payload = new CreateMerchantRequestBuilder().WithDocument(document).Build();
        try
        {
            HttpResponseMessage created = await _client.PostAsJsonAsync("/v1/merchants", payload, CancellationToken.None);
            using JsonDocument createdBody = await TestingWebApplicationFactory.ReadBodyAsync(created);
            Guid id = createdBody.RootElement.GetProperty("id").GetGuid();

            HttpResponseMessage deactivated = await _client.PostAsync($"/v1/merchants/{id}/deactivate", content: null, CancellationToken.None);
            Assert.Equal(HttpStatusCode.NoContent, deactivated.StatusCode);
            Assert.Equal(string.Empty, await deactivated.Content.ReadAsStringAsync(CancellationToken.None));

            HttpResponseMessage readInactive = await _client.GetAsync($"/v1/merchants/{id}", CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, readInactive.StatusCode);
            using JsonDocument inactiveBody = await TestingWebApplicationFactory.ReadBodyAsync(readInactive);
            Assert.False(inactiveBody.RootElement.GetProperty("isActive").GetBoolean());

            // When
            HttpResponseMessage response = await _client.PostAsync($"/v1/merchants/{id}/activate", content: null, CancellationToken.None);

            // Then
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(CancellationToken.None));

            HttpResponseMessage read = await _client.GetAsync($"/v1/merchants/{id}", CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, read.StatusCode);
            using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(read);
            Assert.Equal(id, body.RootElement.GetProperty("id").GetGuid());
            Assert.Equal(payload.Name, body.RootElement.GetProperty("name").GetString());
            Assert.Equal(document, body.RootElement.GetProperty("document").GetString());
            Assert.True(body.RootElement.GetProperty("isActive").GetBoolean());
        }
        finally
        {
            await DeleteByDocumentAsync(document);
        }
    }

    [Fact]
    [Trait("Merchant", "ActivateMerchant (Integration)")]
    public async Task GivenAlreadyActiveMerchant_WhenActivatingAgain_ShouldReturn204WithoutBody()
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
            await _client.PostAsync($"/v1/merchants/{id}/activate", content: null, CancellationToken.None);

            // When
            HttpResponseMessage repeated = await _client.PostAsync($"/v1/merchants/{id}/activate", content: null, CancellationToken.None);

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
    [Trait("Merchant", "ActivateMerchant (Integration)")]
    public async Task GivenUnknownId_WhenActivating_ShouldReturn404ProblemEnvelope()
    {
        // Given
        Guid unknownId = Guid.NewGuid();

        // When
        HttpResponseMessage response = await _client.PostAsync($"/v1/merchants/{unknownId}/activate", content: null, CancellationToken.None);

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
        await using NpgsqlConnection connection = new(_connectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new("DELETE FROM contosopay.merchants WHERE document = @document", connection);
        command.Parameters.AddWithValue("document", document);
        await command.ExecuteNonQueryAsync();
    }
}