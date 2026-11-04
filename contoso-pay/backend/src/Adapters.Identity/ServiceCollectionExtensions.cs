using Adapters.Identity.Models;
using Adapters.Identity.Queries;
using Core.Abstractions.Identity;
using Core.UseCases.AppClients.GetAppClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Validation.AspNetCore;

namespace Adapters.Identity;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentity(
        this IServiceCollection services,
        string connectionString,
        Action<IdentityOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(configure);

        IdentityOptions options = new();
        configure(options);
        Validate(options);

        byte[] signingKey = ResolveSigningKey(options);
        string audience = options.Audience;
        Uri issuer = new(options.Issuer);

        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            options.UseOpenIddict<AppClient, AppClientAuthorization, AppClientScope, AppClientToken, string>();
        });

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<IdentityDbContext>()
                    .ReplaceDefaultEntities<AppClient, AppClientAuthorization, AppClientScope, AppClientToken, string>();
            })
            .AddServer(options =>
            {
                options.AllowClientCredentialsFlow();
                options.SetTokenEndpointUris("connect/token");
                options.SetIssuer(issuer);
                options.SetAccessTokenLifetime(TimeSpan.FromHours(1));
                options.RegisterAudiences(audience);
                options.DisableAccessTokenEncryption();
                options.AddSigningKey(new SymmetricSecurityKey(signingKey));
                options.AddEphemeralSigningKey();
                options.AddEphemeralEncryptionKey();
                options.UseAspNetCore().EnableTokenEndpointPassthrough();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
                options.AddAudiences(audience);
            });

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });
        services.AddAuthorization();

        services.AddScoped<AppClientManager>();
        services.AddScoped<IAppClientProvider, AppClientProvider>();
        services.AddScoped<IAppClientQueries, AppClientQueries>();
        services.AddScoped<IExchangeToken, ExchangeToken>();
        return services;
    }

    private static byte[] ResolveSigningKey(IdentityOptions options)
    {
        string value = options.SigningKey;

        byte[] signingKey;
        try
        {
            signingKey = Convert.FromBase64String(value);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException($"Configuration '{nameof(IdentityOptions.SigningKey)}' must be a Base64 string.", exception);
        }

        if (signingKey.Length < IdentityConstants.SigningKeyMinimumBytes)
        {
            throw new InvalidOperationException($"Configuration '{nameof(IdentityOptions.SigningKey)}' must contain at least {IdentityConstants.SigningKeyMinimumBytes} bytes.");
        }

        return signingKey;
    }

    private static void Validate(IdentityOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SigningKey))
            throw new InvalidOperationException($"Configuration '{nameof(IdentityOptions.SigningKey)}' is required.");

        if (string.IsNullOrWhiteSpace(options.Audience))
            throw new InvalidOperationException($"Configuration '{nameof(IdentityOptions.Audience)}' is required.");

        if (string.IsNullOrWhiteSpace(options.Issuer))
            throw new InvalidOperationException($"Configuration '{nameof(IdentityOptions.Issuer)}' is required.");
    }
}