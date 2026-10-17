using Microsoft.EntityFrameworkCore;

namespace Adapters.Persistence;

public sealed class ContosoPayDbContext(DbContextOptions<ContosoPayDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("contosopay");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContosoPayDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}