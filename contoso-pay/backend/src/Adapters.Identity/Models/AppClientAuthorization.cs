using OpenIddict.EntityFrameworkCore.Models;

namespace Adapters.Identity.Models;

public sealed class AppClientAuthorization : OpenIddictEntityFrameworkCoreAuthorization<string, AppClient, AppClientToken>;