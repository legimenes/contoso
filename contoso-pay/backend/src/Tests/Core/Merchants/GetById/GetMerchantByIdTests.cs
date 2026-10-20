using Core.UseCases.Merchants;
using Core.UseCases.Merchants.GetById;
using Domain;
using Domain.Core.Results;
using Domain.Resources;
using NSubstitute;
using Tests.Domain;

namespace Tests.Core.Merchants.GetById;

public sealed class GetMerchantByIdTests
{
    [Fact]
    [Trait("Merchant", "GetMerchantById (Unit)")]
    public async Task GivenExistingMerchant_WhenGettingMerchant_ShouldReturnMerchantData()
    {
        // Given
        Merchant merchant = new MerchantBuilder().Build();
        GetMerchantByIdRequest request = new GetMerchantByIdRequestBuilder().WithId(merchant.Id).Build();
        IMerchantRepository repository = Substitute.For<IMerchantRepository>();
        repository.GetByIdAsync(merchant.Id, Arg.Any<CancellationToken>()).Returns(merchant);
        GetMerchantById useCase = new(repository);

        // When
        Result<GetMerchantByIdResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsSuccess);
        Assert.Equal(ResultType.Ok, result.Type);
        Assert.NotNull(result.Value);
        Assert.Equal(merchant.Id, result.Value.Id);
        Assert.Equal(merchant.Name, result.Value.Name);
        Assert.Equal(merchant.Document, result.Value.Document);
        await repository.Received(1).GetByIdAsync(merchant.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Merchant", "GetMerchantById (Unit)")]
    public async Task GivenUnknownId_WhenGettingMerchant_ShouldReturnNotFound()
    {
        // Given
        GetMerchantByIdRequest request = new GetMerchantByIdRequestBuilder().Build();
        IMerchantRepository repository = Substitute.For<IMerchantRepository>();
        repository.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns((Merchant?)null);
        GetMerchantById useCase = new(repository);

        // When
        Result<GetMerchantByIdResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.NotFound, result.Type);
        Assert.NotNull(result.Errors);
        Assert.Equal(Resource.Validation_Merchant_NotFound, Assert.Single(result.Errors).Title);
        await repository.Received(1).GetByIdAsync(request.Id, Arg.Any<CancellationToken>());
    }
}