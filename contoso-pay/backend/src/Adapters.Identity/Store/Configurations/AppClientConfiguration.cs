using Adapters.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adapters.Identity.Store.Configurations;

public sealed class AppClientConfiguration : IEntityTypeConfiguration<AppClient>
{
    public void Configure(EntityTypeBuilder<AppClient> builder)
    {
        builder.ToTable("app_clients");
        builder.Property(appClient => appClient.Id).HasColumnName("id").HasMaxLength(100);
        builder.Property(appClient => appClient.ApplicationType).HasColumnName("application_type").HasMaxLength(50);
        builder.Property(appClient => appClient.ClientId).HasColumnName("client_id").HasMaxLength(100);
        builder.Property(appClient => appClient.ClientSecret).HasColumnName("client_secret");
        builder.Property(appClient => appClient.ClientType).HasColumnName("client_type").HasMaxLength(50);
        builder.Property(appClient => appClient.ConcurrencyToken).HasColumnName("concurrency_token").HasMaxLength(50);
        builder.Property(appClient => appClient.ConsentType).HasColumnName("consent_type").HasMaxLength(50);
        builder.Property(appClient => appClient.DisplayName).HasColumnName("display_name").HasMaxLength(100);
        builder.Property(appClient => appClient.DisplayNames).HasColumnName("display_names");
        builder.Property(appClient => appClient.JsonWebKeySet).HasColumnName("json_web_key_set");
        builder.Property(appClient => appClient.Permissions).HasColumnName("permissions");
        builder.Property(appClient => appClient.PostLogoutRedirectUris).HasColumnName("post_logout_redirect_uris");
        builder.Property(appClient => appClient.Properties).HasColumnName("properties");
        builder.Property(appClient => appClient.RedirectUris).HasColumnName("redirect_uris");
        builder.Property(appClient => appClient.Requirements).HasColumnName("requirements");
        builder.Property(appClient => appClient.Settings).HasColumnName("settings");
        builder.Property(appClient => appClient.MerchantId).HasColumnName("merchant_id").IsRequired();
        builder.HasIndex(appClient => appClient.ClientId).IsUnique().HasDatabaseName("ux_app_clients_client_id");
        builder.HasIndex(appClient => appClient.MerchantId).IsUnique().HasDatabaseName("ux_app_clients_merchant_id");
    }
}