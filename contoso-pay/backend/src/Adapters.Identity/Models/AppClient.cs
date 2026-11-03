using OpenIddict.EntityFrameworkCore.Models;

namespace Adapters.Identity.Models;

public sealed class AppClient : OpenIddictEntityFrameworkCoreApplication<string, AppClientAuthorization, AppClientToken>
{
    public Guid MerchantId { get; set; }
}