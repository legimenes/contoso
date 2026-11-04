using Core.UseCases.AppClients.RotateAppClientSecret;
using Core.Abstractions.Identity;
using Domain.Core.Results;
using Domain.Resources;
using NSubstitute;
using Tests.Core.Abstractions.Identity;
using FluentValidation;
using UseCase = Core.UseCases.AppClients.RotateAppClientSecret;

namespace Tests.Core.UseCases.AppClients.RotateAppClientSecret;

public sealed class RotateAppClientSecretTests
{
    private const string CurrentSecret = "current-secret";

    [Fact]
    [Trait("AppClient", "RotateAppClientSecret (Unit)")]
    public async Task GivenOwnerWithCorrectCurrentSecret_WhenRotatingSecret_ShouldReturnCredentialKeepingClientId()
    {
        // Given
        RotateAppClientSecretRequest request = new RotateAppClientSecretRequestBuilder().WithCurrentSecret(CurrentSecret).Build();
        AppClientCredential owned = new AppClientCredentialBuilder()
            .WithClientId(request.ClientId)
            .WithMerchantId(request.MerchantId)
            .Build();
        CreatedAppClientCredential rotated = new(request.ClientId, "new-generated-secret");
        IAppClientProvider appClients = Substitute.For<IAppClientProvider>();
        appClients.FindByClientIdAsync(request.ClientId, Arg.Any<CancellationToken>()).Returns(owned);
        appClients.ValidateSecretAsync(request.ClientId, CurrentSecret, Arg.Any<CancellationToken>()).Returns(true);
        appClients.RotateSecretAsync(request.ClientId, Arg.Any<CancellationToken>()).Returns(rotated);
        UseCase.RotateAppClientSecret useCase = BuildUseCase(appClients);

        // When
        Result<UseCase.RotateAppClientSecretResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsSuccess);
        Assert.Equal(ResultType.Ok, result.Type);
        Assert.NotNull(result.Value);
        Assert.Equal(request.ClientId, result.Value.ClientId);
        Assert.Equal("new-generated-secret", result.Value.ClientSecret);
    }

    [Fact]
    [Trait("AppClient", "RotateAppClientSecret (Unit)")]
    public async Task GivenUnknownClient_WhenRotatingSecret_ShouldReturnNotFound()
    {
        // Given
        RotateAppClientSecretRequest request = new RotateAppClientSecretRequestBuilder().WithCurrentSecret(CurrentSecret).Build();
        IAppClientProvider appClients = Substitute.For<IAppClientProvider>();
        appClients.FindByClientIdAsync(request.ClientId, Arg.Any<CancellationToken>()).Returns((AppClientCredential?)null);
        UseCase.RotateAppClientSecret useCase = BuildUseCase(appClients);

        // When
        Result<UseCase.RotateAppClientSecretResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.NotFound, result.Type);
        Assert.Equal(Resource.Validation_AppClient_NotFound, Assert.Single(result.Errors!).Title);
    }

    [Fact]
    [Trait("AppClient", "RotateAppClientSecret (Unit)")]
    public async Task GivenForeignMerchantClient_WhenRotatingSecret_ShouldReturnNotFound()
    {
        // Given
        RotateAppClientSecretRequest request = new RotateAppClientSecretRequestBuilder().WithCurrentSecret(CurrentSecret).Build();
        AppClientCredential foreign = new AppClientCredentialBuilder()
            .WithClientId(request.ClientId)
            .WithMerchantId(Guid.NewGuid())
            .Build();
        IAppClientProvider appClients = Substitute.For<IAppClientProvider>();
        appClients.FindByClientIdAsync(request.ClientId, Arg.Any<CancellationToken>()).Returns(foreign);
        UseCase.RotateAppClientSecret useCase = BuildUseCase(appClients);

        // When
        Result<UseCase.RotateAppClientSecretResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.NotFound, result.Type);
        Assert.Equal(Resource.Validation_AppClient_NotFound, Assert.Single(result.Errors!).Title);
    }

    [Fact]
    [Trait("AppClient", "RotateAppClientSecret (Unit)")]
    public async Task GivenWrongCurrentSecret_WhenRotatingSecret_ShouldReturnUnauthorized()
    {
        // Given
        RotateAppClientSecretRequest request = new RotateAppClientSecretRequestBuilder().WithCurrentSecret(CurrentSecret).Build();
        AppClientCredential owned = new AppClientCredentialBuilder()
            .WithClientId(request.ClientId)
            .WithMerchantId(request.MerchantId)
            .Build();
        IAppClientProvider appClients = Substitute.For<IAppClientProvider>();
        appClients.FindByClientIdAsync(request.ClientId, Arg.Any<CancellationToken>()).Returns(owned);
        appClients.ValidateSecretAsync(request.ClientId, CurrentSecret, Arg.Any<CancellationToken>()).Returns(false);
        UseCase.RotateAppClientSecret useCase = BuildUseCase(appClients);

        // When
        Result<UseCase.RotateAppClientSecretResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.Unauthorized, result.Type);
        Assert.Equal(Resource.Validation_AppClient_CurrentSecret_Invalid, Assert.Single(result.Errors!).Title);
    }

    [Fact]
    [Trait("AppClient", "RotateAppClientSecret (Unit)")]
    public async Task GivenBlankCurrentSecret_WhenRotatingSecret_ShouldReturnBadRequest()
    {
        // Given
        RotateAppClientSecretRequest request = new RotateAppClientSecretRequestBuilder().WithCurrentSecret("   ").Build();
        UseCase.RotateAppClientSecret useCase = BuildUseCase();

        // When
        Result<UseCase.RotateAppClientSecretResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.BadRequest, result.Type);
        Assert.Contains(Resource.Validation_AppClient_CurrentSecret_Required, result.Errors!.Select(error => error.Title));
    }

    [Fact]
    [Trait("AppClient", "RotateAppClientSecret (Unit)")]
    public async Task GivenClientRemovedDuringRotation_WhenRotatingSecret_ShouldReturnNotFound()
    {
        // Given
        RotateAppClientSecretRequest request = new RotateAppClientSecretRequestBuilder().WithCurrentSecret(CurrentSecret).Build();
        AppClientCredential owned = new AppClientCredentialBuilder()
            .WithClientId(request.ClientId)
            .WithMerchantId(request.MerchantId)
            .Build();
        IAppClientProvider appClients = Substitute.For<IAppClientProvider>();
        appClients.FindByClientIdAsync(request.ClientId, Arg.Any<CancellationToken>()).Returns(owned);
        appClients.ValidateSecretAsync(request.ClientId, CurrentSecret, Arg.Any<CancellationToken>()).Returns(true);
        appClients.RotateSecretAsync(request.ClientId, Arg.Any<CancellationToken>()).Returns((CreatedAppClientCredential?)null);
        UseCase.RotateAppClientSecret useCase = BuildUseCase(appClients);

        // When
        Result<UseCase.RotateAppClientSecretResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.NotFound, result.Type);
        Assert.Equal(Resource.Validation_AppClient_NotFound, Assert.Single(result.Errors!).Title);
    }

    [Fact]
    [Trait("AppClient", "RotateAppClientSecret (Unit)")]
    public async Task GivenClientIdWithSurroundingSpaces_WhenRotatingSecret_ShouldTrimClientIdBeforeFinding()
    {
        // Given
        string clientId = Guid.NewGuid().ToString("N");
        AppClientCredential owned = new AppClientCredentialBuilder()
            .WithClientId(clientId)
            .WithMerchantId(Guid.NewGuid())
            .Build();
        IAppClientProvider appClients = Substitute.For<IAppClientProvider>();
        appClients.FindByClientIdAsync(clientId, Arg.Any<CancellationToken>()).Returns(owned);
        UseCase.RotateAppClientSecret useCase = BuildUseCase(appClients);

        // When
        await useCase.ExecuteAsync(new RotateAppClientSecretRequestBuilder().WithClientId($" {clientId} ").WithCurrentSecret(CurrentSecret).Build(), CancellationToken.None);

        // Then
        await appClients.Received(1).FindByClientIdAsync(clientId, Arg.Any<CancellationToken>());
    }

    private static UseCase.RotateAppClientSecret BuildUseCase(IAppClientProvider? appClients = null)
    {
        IValidator<RotateAppClientSecretRequest> validator = new UseCase.RotateAppClientSecretRequestValidator();
        return new(validator, appClients ?? Substitute.For<IAppClientProvider>());
    }
}
