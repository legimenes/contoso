using Adapters.Identity.AppClients;
using Adapters.Identity.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Core;

namespace Adapters.Identity;

public sealed class AppClientManager(
    IOpenIddictApplicationCache<AppClient> cache,
    ILogger<OpenIddictApplicationManager<AppClient>> logger,
    IOptionsMonitor<OpenIddictCoreOptions> options,
    IOpenIddictApplicationStore<AppClient> store)
    : OpenIddictApplicationManager<AppClient>(cache, logger, options, store)
{
    public override async ValueTask PopulateAsync(
        AppClient application,
        OpenIddictApplicationDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        await base.PopulateAsync(application, descriptor, cancellationToken);

        if (descriptor is AppClientDescriptor appClientDescriptor)
        {
            application.MerchantId = appClientDescriptor.MerchantId;
        }
    }
}