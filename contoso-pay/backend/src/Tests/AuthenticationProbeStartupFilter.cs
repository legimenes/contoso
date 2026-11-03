using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using OpenIddict.Validation.AspNetCore;
using System.Security.Claims;

namespace Tests;

public sealed class AuthenticationProbeStartupFilter : IStartupFilter
{
    internal const string Path = "/__auth-probe";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            next(app);

            app.Use(async (context, nextMiddleware) =>
            {
                if (!context.Request.Path.Equals(Path, StringComparison.OrdinalIgnoreCase))
                {
                    await nextMiddleware(context);
                    return;
                }

                AuthenticateResult result = await context.AuthenticateAsync(
                    OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
                if (!result.Succeeded || result.Principal is null)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                ClaimsPrincipal principal = result.Principal;
                await context.Response.WriteAsJsonAsync(new
                {
                    sub = principal.FindFirst("sub")?.Value,
                    merchantId = principal.FindFirst("merchant_id")?.Value
                });
            });
        };
    }
}