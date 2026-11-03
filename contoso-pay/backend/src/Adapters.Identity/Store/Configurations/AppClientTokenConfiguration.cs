using Adapters.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adapters.Identity.Store.Configurations;

public sealed class AppClientTokenConfiguration : IEntityTypeConfiguration<AppClientToken>
{
    public void Configure(EntityTypeBuilder<AppClientToken> builder)
    {
        builder.ToTable("app_client_tokens");
        builder.Property(token => token.Id).HasColumnName("id").HasMaxLength(100);
        builder.Property<string>("ApplicationId").HasColumnName("application_id").HasMaxLength(100);
        builder.Property<string>("AuthorizationId").HasColumnName("authorization_id").HasMaxLength(100);
        builder.Property(token => token.ConcurrencyToken).HasColumnName("concurrency_token").HasMaxLength(50);
        builder.Property(token => token.CreationDate).HasColumnName("creation_date");
        builder.Property(token => token.ExpirationDate).HasColumnName("expiration_date");
        builder.Property(token => token.Payload).HasColumnName("payload");
        builder.Property(token => token.Properties).HasColumnName("properties");
        builder.Property(token => token.RedemptionDate).HasColumnName("redemption_date");
        builder.Property(token => token.ReferenceId).HasColumnName("reference_id").HasMaxLength(100);
        builder.Property(token => token.Status).HasColumnName("status").HasMaxLength(50);
        builder.Property(token => token.Subject).HasColumnName("subject").HasMaxLength(400);
        builder.Property(token => token.Type).HasColumnName("type").HasMaxLength(150);
        builder.HasIndex(token => token.ReferenceId).IsUnique().HasDatabaseName("ux_app_client_tokens_reference_id");
        builder.HasIndex("ApplicationId", nameof(AppClientToken.Status), nameof(AppClientToken.Subject), nameof(AppClientToken.Type)).HasDatabaseName("ix_app_client_tokens_application_status_subject_type");
    }
}