using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Core.Extensions;
using Domain.Resources;
using Npgsql;

namespace Tests.Core.UseCases.Merchants.ListMerchants;

public sealed class ListMerchantsEndpointTests(TestingWebApplicationFactory factory) : IClassFixture<TestingWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private readonly string _connectionString = TestingWebApplicationFactory.GetConnectionString(factory);

    [Fact]
    [Trait("Merchant", "ListMerchants (Integration)")]
    public async Task GivenSeededMerchants_WhenListingWithDefaultPagination_ShouldReturnEnvelopeOrderedByName()
    {
        // Given
        string prefix = Guid.NewGuid().ToString("N")[..8].ToLowerInvariant();
        string firstDocument = NewDocument();
        string secondDocument = NewDocument();
        string thirdDocument = NewDocument();
        await SeedMerchantAsync($"{prefix} AZUL", firstDocument);
        await SeedMerchantAsync($"{prefix} branco", secondDocument);
        await SeedMerchantAsync($"{prefix} CINZA", thirdDocument);
        try
        {
            // When
            HttpResponseMessage response = await _client.GetAsync($"/v1/merchants?name={prefix}", CancellationToken.None);

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(response);
            Assert.Equal(1, body.RootElement.GetProperty("page").GetInt32());
            Assert.Equal(20, body.RootElement.GetProperty("pageSize").GetInt32());
            Assert.Equal(3, body.RootElement.GetProperty("totalItems").GetInt32());
            Assert.Equal(1, body.RootElement.GetProperty("totalPages").GetInt32());
            JsonElement items = body.RootElement.GetProperty("items");
            Assert.Equal(3, items.GetArrayLength());
            Assert.Equal($"{prefix} AZUL", items[0].GetProperty("name").GetString());
            Assert.Equal($"{prefix} branco", items[1].GetProperty("name").GetString());
            Assert.Equal($"{prefix} CINZA", items[2].GetProperty("name").GetString());
            Assert.False(items[0].TryGetProperty("createdAtUtc", out _));
        }
        finally
        {
            await DeleteByDocumentAsync(firstDocument, secondDocument, thirdDocument);
        }
    }

    [Fact]
    [Trait("Merchant", "ListMerchants (Integration)")]
    public async Task GivenNamePrefixFormattedDifferently_WhenListing_ShouldMatchCaseInsensitively()
    {
        // Given
        string prefix = Guid.NewGuid().ToString("N")[..8];
        string document = NewDocument();
        await SeedMerchantAsync($"{prefix} AZUL", document);
        try
        {
            // When
            HttpResponseMessage response = await _client.GetAsync($"/v1/merchants?name={prefix.ToUpperInvariant()}", CancellationToken.None);

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(response);
            Assert.Equal(1, body.RootElement.GetProperty("totalItems").GetInt32());
            Assert.Equal($"{prefix} AZUL", body.RootElement.GetProperty("items")[0].GetProperty("name").GetString());
        }
        finally
        {
            await DeleteByDocumentAsync(document);
        }
    }

    [Fact]
    [Trait("Merchant", "ListMerchants (Integration)")]
    public async Task GivenExactDocumentFilter_WhenListing_ShouldReturnOnlyMatchingMerchant()
    {
        // Given
        string matchDocument = "55500000000001";
        string otherDocument = "66600000000002";
        try
        {
            await SeedMerchantAsync(Guid.NewGuid().ToString("N")[..8] + " ALFA", matchDocument);
            await SeedMerchantAsync(Guid.NewGuid().ToString("N")[..8] + " BETA", otherDocument);

            // When
            HttpResponseMessage response = await _client.GetAsync($"/v1/merchants?document={matchDocument}", CancellationToken.None);

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(response);
            Assert.Equal(1, body.RootElement.GetProperty("totalItems").GetInt32());
            Assert.Equal(matchDocument, body.RootElement.GetProperty("items")[0].GetProperty("document").GetString());
        }
        finally
        {
            await DeleteByDocumentAsync(matchDocument, otherDocument);
        }
    }

    [Fact]
    [Trait("Merchant", "ListMerchants (Integration)")]
    public async Task GivenDocumentAndNameFilters_WhenListing_ShouldCombineWithAnd()
    {
        // Given
        string prefix = Guid.NewGuid().ToString("N")[..8].ToLowerInvariant();
        string matchDocument = NewDocument();
        string otherDocument = NewDocument();
        await SeedMerchantAsync($"{prefix} GAMMA", matchDocument);
        await SeedMerchantAsync($"{prefix} DELTA", otherDocument);
        try
        {
            // When
            HttpResponseMessage response = await _client.GetAsync($"/v1/merchants?document={matchDocument}&name={prefix}", CancellationToken.None);

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(response);
            Assert.Equal(1, body.RootElement.GetProperty("totalItems").GetInt32());
            Assert.Equal($"{prefix} GAMMA", body.RootElement.GetProperty("items")[0].GetProperty("name").GetString());
            Assert.Equal(matchDocument, body.RootElement.GetProperty("items")[0].GetProperty("document").GetString());
        }
        finally
        {
            await DeleteByDocumentAsync(matchDocument, otherDocument);
        }
    }

    [Fact]
    [Trait("Merchant", "ListMerchants (Integration)")]
    public async Task GivenPrefixWithLikeWildcards_WhenListing_ShouldTreatWildcardsAsLiterals()
    {
        // Given
        string prefix = Guid.NewGuid().ToString("N")[..8].ToLowerInvariant();
        string underscoreDocument = NewDocument();
        string plainDocument = NewDocument();
        await SeedMerchantAsync($"{prefix} A_1% B", underscoreDocument);
        await SeedMerchantAsync($"{prefix} AX", plainDocument);
        try
        {
            // When: '_' as a wildcard would match "{prefix} AX"; as literal it matches nothing else.
            HttpResponseMessage response = await _client.GetAsync($"/v1/merchants?name={Uri.EscapeDataString(prefix + " A_")}", CancellationToken.None);

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(response);
            Assert.Equal(0, body.RootElement.GetProperty("totalItems").GetInt32());
            Assert.Empty(body.RootElement.GetProperty("items").EnumerateArray());
        }
        finally
        {
            await DeleteByDocumentAsync(underscoreDocument, plainDocument);
        }
    }

    [Fact]
    [Trait("Merchant", "ListMerchants (Integration)")]
    public async Task GivenPageBeyondLast_WhenListing_ShouldReturnOkWithEmptyItems()
    {
        // Given
        string prefix = Guid.NewGuid().ToString("N")[..8].ToLowerInvariant();
        string firstDocument = NewDocument();
        string secondDocument = NewDocument();
        await SeedMerchantAsync($"{prefix} EPSILON", firstDocument);
        await SeedMerchantAsync($"{prefix} ZETA", secondDocument);
        try
        {
            // When
            HttpResponseMessage response = await _client.GetAsync($"/v1/merchants?name={prefix}&page=2&pageSize=2", CancellationToken.None);

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(response);
            Assert.Equal(2, body.RootElement.GetProperty("page").GetInt32());
            Assert.Equal(2, body.RootElement.GetProperty("totalItems").GetInt32());
            Assert.Equal(1, body.RootElement.GetProperty("totalPages").GetInt32());
            Assert.Empty(body.RootElement.GetProperty("items").EnumerateArray());
        }
        finally
        {
            await DeleteByDocumentAsync(firstDocument, secondDocument);
        }
    }

    [Fact]
    [Trait("Merchant", "ListMerchants (Integration)")]
    public async Task GivenNoMatchingMerchant_WhenListing_ShouldReturnZeroTotalPages()
    {
        // Given
        string prefix = Guid.NewGuid().ToString("N")[..8].ToLowerInvariant();

        // When
        HttpResponseMessage response = await _client.GetAsync($"/v1/merchants?name={prefix}", CancellationToken.None);

        // Then
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(response);
        Assert.Equal(0, body.RootElement.GetProperty("totalItems").GetInt32());
        Assert.Equal(0, body.RootElement.GetProperty("totalPages").GetInt32());
        Assert.Empty(body.RootElement.GetProperty("items").EnumerateArray());
    }

    [Fact]
    [Trait("Merchant", "ListMerchants (Integration)")]
    public async Task GivenPaginationSplit_WhenListingSequentially_ShouldNotRepeatOrLoseRegisters()
    {
        // Given
        string prefix = Guid.NewGuid().ToString("N")[..8].ToLowerInvariant();
        string[] documents = [NewDocument(), NewDocument(), NewDocument(), NewDocument()];
        string[] names = [$"{prefix} 01", $"{prefix} 02", $"{prefix} 03", $"{prefix} 04"];
        for (int index = 0; index < documents.Length; index++)
        {
            await SeedMerchantAsync(names[index], documents[index]);
        }
        try
        {
            // When
            HttpResponseMessage firstPage = await _client.GetAsync($"/v1/merchants?name={prefix}&page=1&pageSize=3", CancellationToken.None);
            HttpResponseMessage secondPage = await _client.GetAsync($"/v1/merchants?name={prefix}&page=2&pageSize=3", CancellationToken.None);

            // Then
            Assert.Equal(HttpStatusCode.OK, firstPage.StatusCode);
            Assert.Equal(HttpStatusCode.OK, secondPage.StatusCode);
            using JsonDocument firstBody = await TestingWebApplicationFactory.ReadBodyAsync(firstPage);
            using JsonDocument secondBody = await TestingWebApplicationFactory.ReadBodyAsync(secondPage);
            Assert.Equal(4, firstBody.RootElement.GetProperty("totalItems").GetInt32());
            Assert.Equal(2, firstBody.RootElement.GetProperty("totalPages").GetInt32());
            Assert.Equal(2, secondBody.RootElement.GetProperty("totalPages").GetInt32());
            JsonElement firstItems = firstBody.RootElement.GetProperty("items");
            JsonElement secondItems = secondBody.RootElement.GetProperty("items");
            Assert.Equal(3, firstItems.GetArrayLength());
            Assert.Equal(1, secondItems.GetArrayLength());
            HashSet<string> foundDocuments = [];
            for (int index = 0; index < 3; index++)
            {
                Assert.True(foundDocuments.Add(firstItems[index].GetProperty("document").GetString()!));
            }
            Assert.True(foundDocuments.Add(secondItems[0].GetProperty("document").GetString()!));
            foreach (string document in documents)
            {
                Assert.Contains(document, foundDocuments);
            }
        }
        finally
        {
            await DeleteByDocumentAsync(documents);
        }
    }

    [Fact]
    [Trait("Merchant", "ListMerchants (Integration)")]
    public async Task GivenPageBelowMinimum_WhenListing_ShouldReturn400ProblemEnvelopeWithResourceMessage()
    {
        // When
        HttpResponseMessage response = await _client.GetAsync("/v1/merchants?page=0", CancellationToken.None);

        // Then
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using JsonDocument body = await TestingWebApplicationFactory.ReadBodyAsync(response);
        Assert.Equal(400, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("BadRequest", body.RootElement.GetProperty("type").GetString());
        JsonElement errors = body.RootElement.GetProperty("errors");
        Assert.Equal(1, errors.GetArrayLength());
        Assert.Equal(Resource.Validation_Page_Minimum.Format(1), errors[0].GetProperty("title").GetString());
    }

    private async Task SeedMerchantAsync(string name, string document)
    {
        var payload = new { name, document };
        HttpResponseMessage response = await _client.PostAsJsonAsync("/v1/merchants", payload, CancellationToken.None);
        if (response.StatusCode != HttpStatusCode.Created)
        {
            string body = await response.Content.ReadAsStringAsync(CancellationToken.None);
            throw new InvalidOperationException($"Seeding merchant failed: {(int)response.StatusCode} {body}");
        }
    }

    private static string NewDocument()
    {
        return Random.Shared.NextInt64(10_000_000_000L, 100_000_000_000_000L).ToString();
    }

    private async Task DeleteByDocumentAsync(params string[] documents)
    {
        await using NpgsqlConnection connection = new(_connectionString);
        await connection.OpenAsync();
        foreach (string document in documents)
        {
            await using NpgsqlCommand command = new("DELETE FROM contosopay.merchants WHERE document = @document", connection);
            command.Parameters.AddWithValue("document", document);
            await command.ExecuteNonQueryAsync();
        }
    }
}