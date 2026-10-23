using Core.Abstractions.Pagination;
using Core.UseCases.Merchants;
using Core.UseCases.Merchants.ListMerchants;
using Domain.Core.Extensions;
using Domain.Core.Results;
using Domain.Resources;
using NSubstitute;
using Tests.Core.Abstractions;
using UseCase = Core.UseCases.Merchants.ListMerchants;

namespace Tests.Core.UseCases.Merchants.ListMerchants;

public sealed class ListMerchantsTests
{
    [Fact]
    [Trait("Merchant", "ListMerchants (Unit)")]
    public async Task GivenNoFilters_WhenListing_ShouldUseDefaultsAndReturnPassedThroughEnvelope()
    {
        // Given
        PagedResponse<ListMerchantsResponse> merchants = new PagedResponseBuilder<ListMerchantsResponse>().Build();
        IMerchantQueries queries = Substitute.For<IMerchantQueries>();
        queries.SearchAsync(Arg.Any<ListMerchantsRequest>(), Arg.Any<CancellationToken>()).Returns(merchants);
        UseCase.ListMerchants useCase = new(new ListMerchantsRequestValidator(), queries);

        // When
        Result<PagedResponse<ListMerchantsResponse>> result = await useCase.ExecuteAsync(new ListMerchantsRequestBuilder().Build(), CancellationToken.None);

        // Then
        Assert.True(result.IsSuccess);
        Assert.Equal(ResultType.Ok, result.Type);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(20, result.Value.PageSize);
        Assert.Equal(0, result.Value.TotalItems);
        Assert.Equal(0, result.Value.TotalPages);
        Assert.Empty(result.Value.Items);
        await queries.Received(1).SearchAsync(
            Arg.Is<ListMerchantsRequest>(query => query.Document == null && query.Name == null && query.Page == 1 && query.PageSize == 20),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Merchant", "ListMerchants (Unit)")]
    public async Task GivenPageBeyondEnd_WhenListing_ShouldReturnOkWithEmptyItems()
    {
        // Given
        PagedResponse<ListMerchantsResponse> merchants = new PagedResponseBuilder<ListMerchantsResponse>()
            .WithPage(5)
            .WithPageSize(10)
            .WithTotalItems(3)
            .WithTotalPages(1)
            .Build();
        IMerchantQueries queries = Substitute.For<IMerchantQueries>();
        queries.SearchAsync(Arg.Any<ListMerchantsRequest>(), Arg.Any<CancellationToken>()).Returns(merchants);
        UseCase.ListMerchants useCase = new(new ListMerchantsRequestValidator(), queries);

        // When
        Result<PagedResponse<ListMerchantsResponse>> result = await useCase.ExecuteAsync(
            new ListMerchantsRequestBuilder().WithPage(5).WithPageSize(10).Build(), CancellationToken.None);

        // Then
        Assert.True(result.IsSuccess);
        Assert.Equal(ResultType.Ok, result.Type);
        Assert.NotNull(result.Value);
        Assert.Equal(5, result.Value.Page);
        Assert.Equal(10, result.Value.PageSize);
        Assert.Equal(3, result.Value.TotalItems);
        Assert.Equal(1, result.Value.TotalPages);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    [Trait("Merchant", "ListMerchants (Unit)")]
    public async Task GivenFilters_WhenListing_ShouldPassTrimmedFiltersAndPassThroughItems()
    {
        // Given
        IReadOnlyList<ListMerchantsResponse> items =
        [
            new ListMerchantsResponse(Guid.NewGuid(), "Loja Um", "01111111111111"),
            new ListMerchantsResponse(Guid.NewGuid(), "Loja Dois", "02222222222222")
        ];
        PagedResponse<ListMerchantsResponse> merchants = new PagedResponseBuilder<ListMerchantsResponse>()
            .WithPage(2)
            .WithPageSize(50)
            .WithTotalItems(2)
            .WithTotalPages(1)
            .WithItems(items)
            .Build();
        IMerchantQueries queries = Substitute.For<IMerchantQueries>();
        queries.SearchAsync(Arg.Any<ListMerchantsRequest>(), Arg.Any<CancellationToken>()).Returns(merchants);
        UseCase.ListMerchants useCase = new(new ListMerchantsRequestValidator(), queries);

        // When
        Result<PagedResponse<ListMerchantsResponse>> result = await useCase.ExecuteAsync(
            new ListMerchantsRequestBuilder().WithDocument(" 123 ").WithName(" Loja ").WithPage(2).WithPageSize(50).Build(), CancellationToken.None);

        // Then
        Assert.True(result.IsSuccess);
        Assert.Equal(ResultType.Ok, result.Type);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Page);
        Assert.Equal(50, result.Value.PageSize);
        Assert.Equal(2, result.Value.TotalItems);
        Assert.Equal(1, result.Value.TotalPages);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(items[0].Id, result.Value.Items[0].Id);
        Assert.Equal(items[0].Name, result.Value.Items[0].Name);
        Assert.Equal(items[0].Document, result.Value.Items[0].Document);
        Assert.Equal(items[1].Id, result.Value.Items[1].Id);
        Assert.Equal(items[1].Name, result.Value.Items[1].Name);
        Assert.Equal(items[1].Document, result.Value.Items[1].Document);
        await queries.Received(1).SearchAsync(
            Arg.Is<ListMerchantsRequest>(query => query.Document == "123" && query.Name == "Loja" && query.Page == 2 && query.PageSize == 50),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Merchant", "ListMerchants (Unit)")]
    public async Task GivenPageBelowMinimum_WhenListing_ShouldReturnBadRequestAndSkipQueries()
    {
        // Given
        IMerchantQueries queries = Substitute.For<IMerchantQueries>();
        UseCase.ListMerchants useCase = new(new ListMerchantsRequestValidator(), queries);

        // When
        Result<PagedResponse<ListMerchantsResponse>> result = await useCase.ExecuteAsync(
            new ListMerchantsRequestBuilder().WithPage(0).Build(), CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.BadRequest, result.Type);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, error => error.Title == Resource.Validation_Page_Minimum.Format(1));
        await queries.DidNotReceive().SearchAsync(Arg.Any<ListMerchantsRequest>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [Trait("Merchant", "ListMerchants (Unit)")]
    public async Task GivenPageSizeOutOfRange_WhenListing_ShouldReturnBadRequestAndSkipQueries(int pageSize)
    {
        // Given
        IMerchantQueries queries = Substitute.For<IMerchantQueries>();
        UseCase.ListMerchants useCase = new(new ListMerchantsRequestValidator(), queries);

        // When
        Result<PagedResponse<ListMerchantsResponse>> result = await useCase.ExecuteAsync(
            new ListMerchantsRequestBuilder().WithPageSize(pageSize).Build(), CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.BadRequest, result.Type);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, error => error.Title == Resource.Validation_PageSize_Range.Format(1, 100));
        await queries.DidNotReceive().SearchAsync(Arg.Any<ListMerchantsRequest>(), Arg.Any<CancellationToken>());
    }
}