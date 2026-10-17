using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adapters.Persistence.Configurations;

public sealed class MerchantConfiguration : IEntityTypeConfiguration<Merchant>
{
    public void Configure(EntityTypeBuilder<Merchant> builder)
    {
        builder.ToTable("merchants");
        builder.HasKey(merchant => merchant.Id);
        builder.Property(merchant => merchant.Id).HasColumnName("id");
        builder.Property(merchant => merchant.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
        builder.Property(merchant => merchant.Document).HasColumnName("document").IsRequired().HasMaxLength(14);
        builder.HasIndex(merchant => merchant.Document).IsUnique();
        builder.Property(merchant => merchant.CreatedAtUtc).HasColumnName("created_at").IsRequired();
    }
}
