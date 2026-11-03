using OpenIddict.Abstractions;

namespace Adapters.Identity.AppClients;

public sealed class AppClientDescriptor : OpenIddictApplicationDescriptor
{
    public Guid MerchantId { get; set; }
}