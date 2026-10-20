using Core.UseCases.Merchants;
using Core.UseCases.Merchants.Create;
using Domain;
using Domain.Core.Exceptions;
using Domain.Core.Results;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Tests.Core.Merchants.Create;

public sealed class CreateMerchantTests
{
    [Fact]
    [Trait("Merchant", "CreateMerchant (Unit)")]
    public async Task GivenValidRequest_WhenHappyPath_ShouldCreateMerchant()
    {
        // Given
        CreateMerchantRequest request = new CreateMerchantRequestBuilder().Build();
        IMerchantRepository repository = Substitute.For<IMerchantRepository>();
        repository.ExistsByDocumentAsync(request.Document, Arg.Any<CancellationToken>()).Returns(false);
        CreateMerchant useCase = new (repository, new CreateMerchantRequestValidator());

        // When
        Result<CreateMerchantResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsSuccess);
        Assert.Equal(ResultType.Created, result.Type);
        Assert.NotNull(result.Value);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        await repository.Received(1).ExistsByDocumentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await repository.Received(1).AddAsync(Arg.Any<Merchant>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Merchant", "CreateMerchant (Unit)")]
    public async Task GivenBlankName_WhenCreatingMerchant_ShouldReturnBadRequest()
    {
        // Given
        CreateMerchantRequest request = new CreateMerchantRequestBuilder().WithName(string.Empty).Build();
        IMerchantRepository repository = Substitute.For<IMerchantRepository>();
        CreateMerchant useCase = new (repository, new CreateMerchantRequestValidator());

        // When
        Result<CreateMerchantResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.BadRequest, result.Type);
        await repository.DidNotReceive().ExistsByDocumentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await repository.DidNotReceive().AddAsync(Arg.Any<Merchant>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Merchant", "CreateMerchant (Unit)")]
    public async Task GivenBlankDocument_WhenCreatingMerchant_ShouldReturnBadRequest()
    {
        // Given
        CreateMerchantRequest request = new CreateMerchantRequestBuilder().WithDocument(string.Empty).Build();
        IMerchantRepository repository = Substitute.For<IMerchantRepository>();
        CreateMerchant useCase = new (repository, new CreateMerchantRequestValidator());

        // When
        Result<CreateMerchantResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.BadRequest, result.Type);
        await repository.DidNotReceive().ExistsByDocumentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await repository.DidNotReceive().AddAsync(Arg.Any<global::Domain.Merchant>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Merchant", "CreateMerchant (Unit)")]
    public async Task GivenOversizedName_WhenCreatingMerchant_ShouldReturnBadRequest()
    {
        // Given
        CreateMerchantRequest request = new CreateMerchantRequestBuilder()
            .WithName(new string('N', CreateMerchantRequestValidator.NameMaxLength + 1))
            .Build();
        IMerchantRepository repository = Substitute.For<IMerchantRepository>();
        CreateMerchant useCase = new (repository, new CreateMerchantRequestValidator());

        // When
        Result<CreateMerchantResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.BadRequest, result.Type);
        await repository.DidNotReceive().ExistsByDocumentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await repository.DidNotReceive().AddAsync(Arg.Any<Merchant>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Merchant", "CreateMerchant (Unit)")]
    public async Task GivenOversizedDocument_WhenCreatingMerchant_ShouldReturnBadRequest()
    {
        // Given
        CreateMerchantRequest request = new CreateMerchantRequestBuilder()
            .WithDocument(new string('1', CreateMerchantRequestValidator.DocumentMaxLength + 1))
            .Build();
        IMerchantRepository repository = Substitute.For<IMerchantRepository>();
        CreateMerchant useCase = new (repository, new CreateMerchantRequestValidator());

        // When
        Result<CreateMerchantResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.BadRequest, result.Type);
        await repository.DidNotReceive().ExistsByDocumentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await repository.DidNotReceive().AddAsync(Arg.Any<Merchant>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Merchant", "CreateMerchant (Unit)")]
    public async Task GivenExistingDocument_WhenCreatingMerchant_ShouldReturnConflict()
    {
        // Given
        CreateMerchantRequest request = new CreateMerchantRequestBuilder().Build();
        IMerchantRepository repository = Substitute.For<IMerchantRepository>();
        repository.ExistsByDocumentAsync(request.Document, Arg.Any<CancellationToken>()).Returns(true);
        CreateMerchant useCase = new (repository, new CreateMerchantRequestValidator());

        // When
        Result<CreateMerchantResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.Conflict, result.Type);
        await repository.Received(1).ExistsByDocumentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await repository.DidNotReceive().AddAsync(Arg.Any<Merchant>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Merchant", "CreateMerchant (Unit)")]
    public async Task GivenConcurrentDuplicate_WhenRepositoryThrowsDuplicate_ShouldReturnConflict()
    {
        // Given
        CreateMerchantRequest request = new CreateMerchantRequestBuilder().Build();
        IMerchantRepository repository = Substitute.For<IMerchantRepository>();
        repository.ExistsByDocumentAsync(request.Document, Arg.Any<CancellationToken>()).Returns(false);
        repository.AddAsync(Arg.Any<Merchant>(), Arg.Any<CancellationToken>())
            .Throws(new DuplicateMerchantDocumentException(request.Document));
        CreateMerchant useCase = new (repository, new CreateMerchantRequestValidator());

        // When
        Result<CreateMerchantResponse> result = await useCase.ExecuteAsync(request, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.Conflict, result.Type);
        await repository.Received(1).ExistsByDocumentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await repository.Received(1).AddAsync(Arg.Any<Merchant>(), Arg.Any<CancellationToken>());
    }
}