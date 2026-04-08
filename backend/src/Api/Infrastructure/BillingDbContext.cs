using Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure;

public sealed class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options)
{
    public DbSet<CreditWallet> CreditWallets => Set<CreditWallet>();
    public DbSet<CreditTransaction> CreditTransactions => Set<CreditTransaction>();
    public DbSet<CreditReservation> CreditReservations => Set<CreditReservation>();
    public DbSet<StripeWebhookEvent> StripeWebhookEvents => Set<StripeWebhookEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CreditWallet>(builder =>
        {
            builder.ToTable("credit_wallets");
            builder.HasKey(x => x.TenantId);
            builder.Property(x => x.BalanceCredits).IsRequired();
            builder.Ignore(x => x.Tenant);
        });

        modelBuilder.Entity<CreditTransaction>(builder =>
        {
            builder.ToTable("credit_transactions");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.ReferenceId).IsUnique();
            builder.Property(x => x.ReferenceId).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.Description).HasMaxLength(500);
            builder.Ignore(x => x.Tenant);
        });

        modelBuilder.Entity<CreditReservation>(builder =>
        {
            builder.ToTable("credit_reservations");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.ReferenceId).IsUnique();
            builder.Property(x => x.ReferenceId).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            builder.Ignore(x => x.Tenant);
        });

        modelBuilder.Entity<StripeWebhookEvent>(builder =>
        {
            builder.ToTable("stripe_webhook_events");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasMaxLength(100);
            builder.Property(x => x.EventType).HasMaxLength(100).IsRequired();
            builder.Property(x => x.PayloadHash).HasMaxLength(128).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
            builder.Property(x => x.ErrorMessage).HasMaxLength(500);
            builder.HasIndex(x => x.TenantId);
        });
    }
}

