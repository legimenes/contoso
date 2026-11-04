using Core.Abstractions.Identity;
using Core.UseCases.AppClients;
using Core.UseCases.AppClients.CreateAppClient;
using Core.UseCases.Merchants;
using Domain;
using Domain.Core.Exceptions;
using Domain.Core.Extensions;
using Domain.Core.Results;
using Domain.Resources;
using FluentValidation;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Tests.Domain;
using UseCase = Core.UseCases.AppClients.CreateAppClient;

namespace Tests.Core.UseCases.AppClients.CreateAppClient;

public sealed class CreateAppClientTests
{
    [Fact]
    [Trait("AppClient", "CreateAppClient (Unit)")]
    public async Task GivenValidActiveMerchantWithoutAppClient_WhenCreatingAppClient_ShouldReturnCredential()
    {
        // Given
        Merchant merchant = new MerchantBuilder().Build();
        CreatedAppClientCredential credential = new(Guid.NewGuid().ToString("N"), "generated-secret");
        IMerchantRepository merchants = Substitute.For<IMerchantRepository>();
        merchants.GetByIdAsync(merchant.Id, Arg.Any<CancellationToken>()).Returns(merchant);
        IAppClientProvider appClients = Substitute.For<IAppClientProvider>();
        appClients.ExistsByMerchantIdAsync(merchant.Id, Arg.Any<CancellationToken>()).Returns(false);
        appClients
            .CreateAsync(merchant.Id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(credential);
        UseCase.CreateAppClient useCase = BuildUseCase(appClients, merchants);

        // When
        Result<CreateAppClientResponse> result =
            await useCase.ExecuteAsync(new CreateAppClientRequestBuilder().WithMerchantId(merchant.Id).Build(), CancellationToken.None);

        // Then
        Assert.True(result.IsSuccess);
        Assert.Equal(ResultType.Created, result.Type);
        Assert.NotNull(result.Value);
        Assert.Equal(credential.ClientId, result.Value.ClientId);
        Assert.Equal(credential.ClientSecret, result.Value.ClientSecret);
        await appClients.Received(1).CreateAsync(merchant.Id, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("AppClient", "CreateAppClient (Unit)")]
    public async Task GivenUnknownMerchant_WhenCreatingAppClient_ShouldReturnNotFound()
    {
        // Given
        CreateAppClientRequest request = new CreateAppClientRequestBuilder().Build();
        IMerchantRepository merchants = Substitute.For<IMerchantRepository>();
        merchants.GetByIdAsync(request.MerchantId, Arg.Any<CancellationToken>()).Returns((Merchant?)null);
        UseCase.CreateAppClient useCase = BuildUseCase(appClients: null!, merchants: merchants);

        // When
        Result<CreateAppClientResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.NotFound, result.Type);
        Assert.Equal(Resource.Validation_Merchant_NotFound, Assert.Single(result.Errors!).Title);
    }

    [Fact]
    [Trait("AppClient", "CreateAppClient (Unit)")]
    public async Task GivenInactiveMerchant_WhenCreatingAppClient_ShouldReturnConflict()
    {
        // Given
        Merchant merchant = new MerchantBuilder().Inactive().Build();
        CreateAppClientRequest request = new CreateAppClientRequestBuilder().WithMerchantId(merchant.Id).Build();
        IMerchantRepository merchants = Substitute.For<IMerchantRepository>();
        merchants.GetByIdAsync(merchant.Id, Arg.Any<CancellationToken>()).Returns(merchant);
        UseCase.CreateAppClient useCase = BuildUseCase(appClients: null!, merchants: merchants);

        // When
        Result<CreateAppClientResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.Conflict, result.Type);
        Assert.Equal(Resource.Validation_Merchant_Inactive, Assert.Single(result.Errors!).Title);
    }

    [Fact]
    [Trait("AppClient", "CreateAppClient (Unit)")]
    public async Task GivenMerchantWithAppClient_WhenCreatingAppClient_ShouldReturnConflict()
    {
        // Given
        Merchant merchant = new MerchantBuilder().Build();
        CreateAppClientRequest request = new CreateAppClientRequestBuilder().WithMerchantId(merchant.Id).Build();
        IMerchantRepository merchants = Substitute.For<IMerchantRepository>();
        merchants.GetByIdAsync(merchant.Id, Arg.Any<CancellationToken>()).Returns(merchant);
        IAppClientProvider appClients = Substitute.For<IAppClientProvider>();
        appClients.ExistsByMerchantIdAsync(merchant.Id, Arg.Any<CancellationToken>()).Returns(true);
        UseCase.CreateAppClient useCase = BuildUseCase(appClients, merchants);

        // When
        Result<CreateAppClientResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.Conflict, result.Type);
        Assert.Equal(Resource.Validation_AppClient_AlreadyExists, Assert.Single(result.Errors!).Title);
    }

    [Fact]
    [Trait("AppClient", "CreateAppClient (Unit)")]
    public async Task GivenDuplicateRace_WhenCreatingAppClient_ShouldReturnConflict()
    {
        // Given
        Merchant merchant = new MerchantBuilder().Build();
        CreateAppClientRequest request = new CreateAppClientRequestBuilder().WithMerchantId(merchant.Id).Build();
        IMerchantRepository merchants = Substitute.For<IMerchantRepository>();
        merchants.GetByIdAsync(merchant.Id, Arg.Any<CancellationToken>()).Returns(merchant);
        IAppClientProvider appClients = Substitute.For<IAppClientProvider>();
        appClients.ExistsByMerchantIdAsync(merchant.Id, Arg.Any<CancellationToken>()).Returns(false);
        appClients
            .CreateAsync(merchant.Id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateAppClientException(merchant.Id));
        UseCase.CreateAppClient useCase = BuildUseCase(appClients, merchants);

        // When
        Result<CreateAppClientResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.Conflict, result.Type);
        Assert.Equal(Resource.Validation_AppClient_AlreadyExists, Assert.Single(result.Errors!).Title);
    }

    [Fact]
    [Trait("AppClient", "CreateAppClient (Unit)")]
    public async Task GivenBlankName_WhenCreatingAppClient_ShouldReturnBadRequest()
    {
        // Given
        CreateAppClientRequest request = new CreateAppClientRequestBuilder().WithName("  ").Build();
        UseCase.CreateAppClient useCase = BuildUseCase();

        // When
        Result<CreateAppClientResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.BadRequest, result.Type);
        Assert.Contains(Resource.Validation_Name_Required, result.Errors!.Select(error => error.Title));
    }

    [Fact]
    [Trait("AppClient", "CreateAppClient (Unit)")]
    public async Task GivenNameOver100Characters_WhenCreatingAppClient_ShouldReturnBadRequest()
    {
        // Given
        CreateAppClientRequest request = new CreateAppClientRequestBuilder().WithName(new string('a', 101)).Build();
        UseCase.CreateAppClient useCase = BuildUseCase();

        // When
        Result<CreateAppClientResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.BadRequest, result.Type);
        Assert.Contains(Resource.Validation_Name_MaxLength.Format(CreateAppClientRequestValidator.NameMaxLength), result.Errors!.Select(error => error.Title));
    }

    [Fact]
    [Trait("AppClient", "CreateAppClient (Unit)")]
    public async Task GivenEmptyMerchantId_WhenCreatingAppClient_ShouldReturnBadRequest()
    {
        // Given
        CreateAppClientRequest request = new CreateAppClientRequestBuilder().WithMerchantId(Guid.Empty).Build();
        UseCase.CreateAppClient useCase = BuildUseCase();

        // When
        Result<CreateAppClientResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.BadRequest, result.Type);
        Assert.Contains(Resource.Validation_MerchantId_Required, result.Errors!.Select(error => error.Title));
    }

    [Fact]
    [Trait("AppClient", "CreateAppClient (Unit)")]
    public async Task GivenNameWithSurroundingSpaces_WhenCreatingAppClient_ShouldTrimNameBeforeCreating()
    {
        // Given
        Merchant merchant = new MerchantBuilder().Build();
        CreatedAppClientCredential credential = new(Guid.NewGuid().ToString("N"), "generated-secret");
        IMerchantRepository merchants = Substitute.For<IMerchantRepository>();
        merchants.GetByIdAsync(merchant.Id, Arg.Any<CancellationToken>()).Returns(merchant);
        IAppClientProvider appClients = Substitute.For<IAppClientProvider>();
        appClients.ExistsByMerchantIdAsync(merchant.Id, Arg.Any<CancellationToken>()).Returns(false);
        appClients
            .CreateAsync(merchant.Id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(credential);
        UseCase.CreateAppClient useCase = BuildUseCase(appClients, merchants);

        // When
        await useCase.ExecuteAsync(new CreateAppClientRequestBuilder().WithMerchantId(merchant.Id).WithName("  Loja Exemplo  ").Build(), CancellationToken.None);

        // Then
        await appClients.Received(1).CreateAsync(merchant.Id, "Loja Exemplo", Arg.Any<CancellationToken>());
    }

    private static UseCase.CreateAppClient BuildUseCase(
        IAppClientProvider? appClients = null,
        IMerchantRepository? merchants = null)
    {
        IValidator<CreateAppClientRequest> validator = new UseCase.CreateAppClientRequestValidator();
        return new(
            validator,
            merchants ?? Substitute.For<IMerchantRepository>(),
            appClients ?? Substitute.For<IAppClientProvider>());
    }
}
