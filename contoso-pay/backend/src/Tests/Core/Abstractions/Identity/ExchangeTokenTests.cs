using Adapters.Identity;
using Adapters.Identity.AppClients;
using Core.UseCases.Merchants;
using Domain;
using Domain.Core.Results;
using NSubstitute;
using System.Security.Claims;

namespace Tests.Core.Abstractions.Identity;

public sealed class ExchangeTokenTests
{
    private const string ClientSecret = "current-secret";

    [Fact]
    [Trait("AppClient", "ExchangeToken (Unit)")]
    public async Task GivenValidCredentials_WhenExchangingToken_ShouldReturnPrincipalWithSubAndMerchantId()
    {
        // Given
        Merchant merchant = new Tests.Domain.MerchantBuilder().Build();
        AppClientCredential credential = new AppClientCredentialBuilder().WithMerchantId(merchant.Id).Build();
        IAppClientCredentials credentials = Substitute.For<IAppClientCredentials>();
        IMerchantRepository merchants = Substitute.For<IMerchantRepository>();
        credentials.FindByClientIdAsync(credential.ClientId, Arg.Any<CancellationToken>()).Returns(credential);
        credentials.ValidateSecretAsync(credential.ClientId, ClientSecret, Arg.Any<CancellationToken>()).Returns(true);
        merchants.GetByIdAsync(credential.MerchantId, Arg.Any<CancellationToken>()).Returns(merchant);
        ExchangeToken exchangeToken = new (credentials, merchants);

        // When
        Result<ClaimsPrincipal> result = await exchangeToken.ExchangeAsync(credential.ClientId, ClientSecret, CancellationToken.None);

        // Then
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        ClaimsIdentity identity = Assert.IsType<ClaimsIdentity>(Assert.Single(result.Value.Identities));
        Assert.Equal(credential.ClientId, identity.FindFirst("sub")?.Value);
        Assert.Equal(merchant.Id.ToString(), identity.FindFirst("merchant_id")?.Value);
    }

    [Fact]
    [Trait("AppClient", "ExchangeToken (Unit)")]
    public async Task GivenUnknownClient_WhenExchangingToken_ShouldReturnUnauthorized()
    {
        // Given
        IAppClientCredentials credentials = Substitute.For<IAppClientCredentials>();
        IMerchantRepository merchants = Substitute.For<IMerchantRepository>();
        credentials.FindByClientIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((AppClientCredential?)null);
        ExchangeToken exchangeToken = new(credentials, merchants);

        // When
        Result<ClaimsPrincipal> result = await exchangeToken.ExchangeAsync("unknown", ClientSecret, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.Unauthorized, result.Type);
        await credentials.DidNotReceive().ValidateSecretAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("AppClient", "ExchangeToken (Unit)")]
    public async Task GivenWrongSecret_WhenExchangingToken_ShouldReturnUnauthorized()
    {
        // Given
        AppClientCredential credential = new AppClientCredentialBuilder().Build();
        IAppClientCredentials credentials = Substitute.For<IAppClientCredentials>();
        IMerchantRepository merchants = Substitute.For<IMerchantRepository>();
        credentials.FindByClientIdAsync(credential.ClientId, Arg.Any<CancellationToken>()).Returns(credential);
        credentials.ValidateSecretAsync(credential.ClientId, ClientSecret, Arg.Any<CancellationToken>()).Returns(false);
        ExchangeToken exchangeToken = new(credentials, merchants);

        // When
        Result<ClaimsPrincipal> result = await exchangeToken.ExchangeAsync(credential.ClientId, ClientSecret, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.Unauthorized, result.Type);
        await merchants.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("AppClient", "ExchangeToken (Unit)")]
    public async Task GivenMissingMerchant_WhenExchangingToken_ShouldReturnUnauthorized()
    {
        // Given
        AppClientCredential credential = new AppClientCredentialBuilder().Build();
        IAppClientCredentials credentials = Substitute.For<IAppClientCredentials>();
        IMerchantRepository merchants = Substitute.For<IMerchantRepository>();
        credentials.FindByClientIdAsync(credential.ClientId, Arg.Any<CancellationToken>()).Returns(credential);
        credentials.ValidateSecretAsync(credential.ClientId, ClientSecret, Arg.Any<CancellationToken>()).Returns(true);
        merchants.GetByIdAsync(credential.MerchantId, Arg.Any<CancellationToken>()).Returns((Merchant?)null);
        ExchangeToken exchangeToken = new(credentials, merchants);

        // When
        Result<ClaimsPrincipal> result = await exchangeToken.ExchangeAsync(credential.ClientId, ClientSecret, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.Unauthorized, result.Type);
    }

    [Fact]
    [Trait("AppClient", "ExchangeToken (Unit)")]
    public async Task GivenInactiveMerchant_WhenExchangingToken_ShouldReturnUnauthorized()
    {
        // Given
        Merchant merchant = new Tests.Domain.MerchantBuilder().Inactive().Build();
        AppClientCredential credential = new AppClientCredentialBuilder().WithMerchantId(merchant.Id).Build();
        IAppClientCredentials credentials = Substitute.For<IAppClientCredentials>();
        IMerchantRepository merchants = Substitute.For<IMerchantRepository>();
        credentials.FindByClientIdAsync(credential.ClientId, Arg.Any<CancellationToken>()).Returns(credential);
        credentials.ValidateSecretAsync(credential.ClientId, ClientSecret, Arg.Any<CancellationToken>()).Returns(true);
        merchants.GetByIdAsync(credential.MerchantId, Arg.Any<CancellationToken>()).Returns(merchant);
        ExchangeToken exchangeToken = new(credentials, merchants);

        // When
        Result<ClaimsPrincipal> result = await exchangeToken.ExchangeAsync(credential.ClientId, ClientSecret, CancellationToken.None);

        // Then
        Assert.True(result.IsFailure);
        Assert.Equal(ResultType.Unauthorized, result.Type);
    }
}