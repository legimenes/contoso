using Core.UseCases.AppClients.GetAppClient;
using Domain.Core.Results;
using Domain.Resources;
using FluentValidation;
using NSubstitute;
using UseCase = Core.UseCases.AppClients.GetAppClient;

namespace Tests.Core.UseCases.AppClients.GetAppClient;

public sealed class GetAppClientTests
{
    [Fact]
    [Trait("AppClient", "GetAppClient (Unit)")]
    public async Task GivenOwnedAppClient_WhenGettingAppClient_ShouldReturnClientDataWithoutSecret()
    {
        // Given
        GetAppClientRequest request = new GetAppClientRequestBuilder().Build();
        UseCase.GetAppClientResponse projection = new(request.ClientId, request.MerchantId, "Loja Exemplo");
        IAppClientQueries queries = Substitute.For<IAppClientQueries>();
        queries.GetByClientIdAsync(Arg.Any<GetAppClientRequest>(), Arg.Any<CancellationToken>()).Returns(projection);
        UseCase.GetAppClient useCase = BuildUseCase(queries);

        // When
        Result<UseCase.GetAppClientResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsSuccess);
        Assert.Equal(ResultType.Ok, result.Type);
        Assert.NotNull(result.Value);
        Assert.Equal(request.ClientId, result.Value.ClientId);
        Assert.Equal(request.MerchantId, result.Value.MerchantId);
        Assert.Equal("Loja Exemplo", result.Value.Name);
        await queries.Received(1).GetByClientIdAsync(new GetAppClientRequest(request.ClientId.Trim(), request.MerchantId), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("AppClient", "GetAppClient (Unit)")]
    public async Task GivenUnknownClient_WhenGettingAppClient_ShouldReturnNotFound()
    {
        // Given
        GetAppClientRequest request = new GetAppClientRequestBuilder().Build();
        IAppClientQueries queries = Substitute.For<IAppClientQueries>();
        queries.GetByClientIdAsync(Arg.Any<GetAppClientRequest>(), Arg.Any<CancellationToken>()).Returns((UseCase.GetAppClientResponse?)null);
        UseCase.GetAppClient useCase = BuildUseCase(queries);

        // When
        Result<UseCase.GetAppClientResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.NotFound, result.Type);
        Assert.Equal(Resource.Validation_AppClient_NotFound, Assert.Single(result.Errors!).Title);
    }

    [Fact]
    [Trait("AppClient", "GetAppClient (Unit)")]
    public async Task GivenForeignMerchantClient_WhenGettingAppClient_ShouldReturnNotFound()
    {
        // Given
        GetAppClientRequest request = new GetAppClientRequestBuilder().Build();
        IAppClientQueries queries = Substitute.For<IAppClientQueries>();
        queries.GetByClientIdAsync(Arg.Any<GetAppClientRequest>(), Arg.Any<CancellationToken>()).Returns((UseCase.GetAppClientResponse?)null);
        UseCase.GetAppClient useCase = BuildUseCase(queries);

        // When
        Result<UseCase.GetAppClientResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.NotFound, result.Type);
        Assert.Equal(Resource.Validation_AppClient_NotFound, Assert.Single(result.Errors!).Title);
    }

    [Fact]
    [Trait("AppClient", "GetAppClient (Unit)")]
    public async Task GivenBlankClientId_WhenGettingAppClient_ShouldReturnBadRequest()
    {
        // Given
        GetAppClientRequest request = new GetAppClientRequestBuilder().WithClientId("   ").Build();
        UseCase.GetAppClient useCase = BuildUseCase(Substitute.For<IAppClientQueries>());

        // When
        Result<UseCase.GetAppClientResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.BadRequest, result.Type);
        Assert.Contains(Resource.Validation_ClientId_Required, result.Errors!.Select(error => error.Title));
    }

    private static UseCase.GetAppClient BuildUseCase(IAppClientQueries queries)
    {
        IValidator<GetAppClientRequest> validator = new UseCase.GetAppClientRequestValidator();
        return new(validator, queries);
    }
}
