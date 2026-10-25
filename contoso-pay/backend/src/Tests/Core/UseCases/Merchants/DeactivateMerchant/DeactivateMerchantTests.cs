using Core.UseCases.Merchants;
using Core.UseCases.Merchants.DeactivateMerchant;
using Domain;
using Domain.Core.Results;
using Domain.Resources;
using NSubstitute;
using Tests.Domain;
using UseCase = Core.UseCases.Merchants.DeactivateMerchant;

namespace Tests.Core.UseCases.Merchants.DeactivateMerchant;

public sealed class DeactivateMerchantTests
{
    [Fact]
    [Trait("Merchant", "DeactivateMerchant (Unit)")]
    public async Task GivenExistingMerchant_WhenDeactivating_ShouldReturnNoContentAndCommitOnce()
    {
        // Given
        Merchant merchant = new MerchantBuilder().Build();
        DeactivateMerchantRequest request = new DeactivateMerchantRequestBuilder().WithId(merchant.Id).Build();
        IMerchantRepository repository = Substitute.For<IMerchantRepository>();
        repository.GetByIdAsync(merchant.Id, Arg.Any<CancellationToken>()).Returns(merchant);
        UseCase.DeactivateMerchant useCase = new(repository);

        // When
        Result result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsSuccess);
        Assert.Equal(ResultType.NoContent, result.Type);
        Assert.False(merchant.IsActive);
        await repository.Received(1).GetByIdAsync(merchant.Id, Arg.Any<CancellationToken>());
        await repository.Received(1).UpdateAsync(merchant, Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Merchant", "DeactivateMerchant (Unit)")]
    public async Task GivenUnknownId_WhenDeactivating_ShouldReturnNotFoundAndNotCommit()
    {
        // Given
        DeactivateMerchantRequest request = new DeactivateMerchantRequestBuilder().Build();
        IMerchantRepository repository = Substitute.For<IMerchantRepository>();
        repository.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns((Merchant?)null);
        UseCase.DeactivateMerchant useCase = new(repository);

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