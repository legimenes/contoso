namespace Adapters.Identity;

internal static class IdentityConstants
{
    public const int SigningKeyMinimumBytes = 32;
    public const string Audience = "contoso-pay-api";
    public const string MerchantIdClaim = "merchant_id";
}