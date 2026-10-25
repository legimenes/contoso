using Core.UseCases.Merchants;
using Core.UseCases.Merchants.ActivateMerchant;
using Domain;
using Domain.Core.Results;
using Domain.Resources;
using NSubstitute;
using Tests.Domain;
using UseCase = Core.UseCases.Merchants.ActivateMerchant;

namespace Tests.Core.UseCases.Merchants.ActivateMerchant;

public sealed class ActivateMerchantTests
{
    [Fact]
    [Trait("Merchant", "ActivateMerchant (Unit)")]
    public async Task GivenInactiveMerchant_WhenActivating_ShouldReturnNoContentAndCommitOnce()
    {
        // Given
        Merchant merchant = new MerchantBuilder().Inactive().Build();
        ActivateMerchantRequest request = new ActivateMerchantRequestBuilder().WithId(merchant.Id).Build();
        IMerchantRepository repository = Substitute.For<IMerchantRepository>();
        repository.GetByIdAsync(merchant.Id, Arg.Any<CancellationToken>()).Returns(merchant);
        UseCase.ActivateMerchant useCase = new(repository);

        // When
        Result result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsSuccess);
        Assert.Equal(ResultType.NoContent, result.Type);
        Assert.True(merchant.IsActive);
        await repository.Received(1).GetByIdAsync(merchant.Id, Arg.Any<CancellationToken>());
        await repository.Received(1).UpdateAsync(merchant, Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Merchant", "ActivateMerchant (Unit)")]
    public async Task GivenUnknownId_WhenActivating_ShouldReturnNotFoundAndNotCommit()
    {
        // Given
        ActivateMerchantRequest request = new ActivateMerchantRequestBuilder().Build();
        IMerchantRepository repository = Substitute.For<IMerchantRepository>();
        repository.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns((Merchant?)null);
        UseCase.ActivateMerchant useCase = new(repository);

        // When
        Result result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.NotFound, result.Type);
        Assert.NotNull(result.Errors);
        Assert.Equal(Resource.Validation_Merchant_NotFound, Assert.Single(result.Errors).Title);
        await repository.Received(1).GetByIdAsync(request.Id, Arg.Any<CancellationToken>());
        await repository.DidNotReceive().UpdateAsync(Arg.Any<Merchant>(), Arg.Any<CancellationToken>());
    }
}