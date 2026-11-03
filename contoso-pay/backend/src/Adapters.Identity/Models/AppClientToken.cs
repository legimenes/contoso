using OpenIddict.EntityFrameworkCore.Models;

namespace Adapters.Identity.Models;

public sealed class AppClientToken : OpenIddictEntityFrameworkCoreToken<string, AppClient, AppClientAuthorization>;