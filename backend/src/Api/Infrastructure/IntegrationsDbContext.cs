using Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure;

public sealed class IntegrationsDbContext(DbContextOptions<IntegrationsDbContext> options) : DbContext(options)
{
    public DbSet<LinkedInOAuthState> LinkedInOAuthStates => Set<LinkedInOAuthState>();
    public DbSet<LinkedInConnection> LinkedInConnections => Set<LinkedInConnection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LinkedInOAuthState>(builder =>
        {
            builder.ToTable("linkedin_oauth_states");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.State).IsUnique();
            builder.Property(x => x.State).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<LinkedInConnection>(builder =>
        {
            builder.ToTable("linkedin_connections");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.TenantId).IsUnique();
            builder.Property(x => x.AccessToken).HasMaxLength(4000).IsRequired();
            builder.Property(x => x.Scope).HasMaxLength(500);
            builder.Property(x => x.MemberUrn).HasMaxLength(200);
        });
    }
}

