using Domain;

namespace Tests.Domain;

public sealed class MerchantTests
{
    [Fact]
    [Trait("Merchant", "Merchant (Domain)")]
    public void New_WithValidData_ShouldAssignIdentity()
    {
        // Given
        Merchant merchantFromBuilder = new MerchantBuilder().Build();
        DateTime before = DateTime.UtcNow.AddSeconds(-5);

        // When
        Merchant merchant = Merchant.New(
            name: merchantFromBuilder.Name,
            document: merchantFromBuilder.Document);

        // Then
        Assert.NotEqual(Guid.Empty, merchant.Id);
        Assert.Equal(merchantFromBuilder.Name, merchant.Name);
        Assert.Equal(merchantFromBuilder.Document, merchant.Document);
        Assert.True(merchant.CreatedAtUtc >= before);
    }

    [Fact]
    [Trait("Merchant", "Merchant (Domain)")]
    public void New_WhenMerchantIsCreated_ShouldStartActive()
    {
        // Given
        MerchantBuilder builder = new MerchantBuilder();

        // When
        Merchant merchant = builder.Build();

        // Then
        Assert.True(merchant.IsActive);
    }

    [Fact]
    [Trait("Merchant", "Merchant (Domain)")]
    public void Deactivate_WhenActive_ShouldBecomeInactive()
    {
        // Given
        Merchant merchant = new MerchantBuilder().Build();

        // When
        merchant.Deactivate();

        // Then
        Assert.False(merchant.IsActive);
    }

    [Fact]
    [Trait("Merchant", "Merchant (Domain)")]
    public void Activate_WhenInactive_ShouldBecomeActive()
    {
        // Given
        Merchant merchant = new MerchantBuilder().Inactive().Build();

        // When
        merchant.Activate();

        // Then
        Assert.True(merchant.IsActive);
    }

    [Fact]
    [Trait("Merchant", "Merchant (Domain)")]
    public void Deactivate_WhenAlreadyInactive_ShouldRemainInactive()
    {
        // Given
        Merchant merchant = new MerchantBuilder().Inactive().Build();

        // When
        merchant.Deactivate();

        // Then
        Assert.False(merchant.IsActive);
    }

    [Fact]
    [Trait("Merchant", "Merchant (Domain)")]
    public void Activate_WhenAlreadyActive_ShouldRemainActive()
    {
        // Given
        Merchant merchant = new MerchantBuilder().Build();

        // When
        merchant.Activate();

        // Then
        Assert.True(merchant.IsActive);
    }

    [Fact]
    [Trait("Merchant", "Merchant (Domain)")]
    public void New_WithValidData_ShouldTrimAndAssignIdentity()
    {
        // Given
        Merchant merchantFromBuilder = new MerchantBuilder()
            .WithName("  Loja Exemplo  ")
            .WithDocument("  12345678000190  ")
            .Build();
        DateTime before = DateTime.UtcNow.AddSeconds(-5);

        // When
        Merchant merchant = Merchant.New(
            name: merchantFromBuilder.Name,
            document: merchantFromBuilder.Document);

        // Then
        Assert.NotEqual(Guid.Empty, merchant.Id);
        Assert.Equal(merchantFromBuilder.Name.Trim(), merchant.Name);
        Assert.Equal(merchantFromBuilder.Document.Trim(), merchant.Document);
        Assert.True(merchant.CreatedAtUtc >= before);
    }
}