using Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<CreditWallet> CreditWallets => Set<CreditWallet>();
    public DbSet<CreditTransaction> CreditTransactions => Set<CreditTransaction>();
    public DbSet<CreditReservation> CreditReservations => Set<CreditReservation>();
    public DbSet<ScheduledPost> ScheduledPosts => Set<ScheduledPost>();
    public DbSet<PostTarget> PostTargets => Set<PostTarget>();
    public DbSet<StripeWebhookEvent> StripeWebhookEvents => Set<StripeWebhookEvent>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<ScheduledPostMedia> ScheduledPostMediaLinks => Set<ScheduledPostMedia>();
    public DbSet<LinkedInOAuthState> LinkedInOAuthStates => Set<LinkedInOAuthState>();
    public DbSet<LinkedInConnection> LinkedInConnections => Set<LinkedInConnection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Email).IsUnique();
            builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
            builder.Property(x => x.PasswordHash).IsRequired();
            builder.Property(x => x.PasswordSalt).HasColumnName("passwordsalt").IsRequired();
        });

        modelBuilder.Entity<Tenant>(builder =>
        {
            builder.ToTable("tenants");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Membership>(builder =>
        {
            builder.ToTable("memberships");
            builder.HasKey(x => new { x.UserId, x.TenantId });
            builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            builder.HasOne(x => x.User)
                .WithMany(x => x.Memberships)
                .HasForeignKey(x => x.UserId);
            builder.HasOne(x => x.Tenant)
                .WithMany(x => x.Memberships)
                .HasForeignKey(x => x.TenantId);
        });

        modelBuilder.Entity<CreditWallet>(builder =>
        {
            builder.ToTable("credit_wallets");
            builder.HasKey(x => x.TenantId);
            builder.Property(x => x.BalanceCredits).IsRequired();
            builder.HasOne(x => x.Tenant)
                .WithOne()
                .HasForeignKey<CreditWallet>(x => x.TenantId);
        });

        modelBuilder.Entity<CreditTransaction>(builder =>
        {
            builder.ToTable("credit_transactions");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.ReferenceId).IsUnique();
            builder.Property(x => x.ReferenceId).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.Description).HasMaxLength(500);
            builder.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId);
        });

        modelBuilder.Entity<CreditReservation>(builder =>
        {
            builder.ToTable("credit_reservations");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.ReferenceId).IsUnique();
            builder.Property(x => x.ReferenceId).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            builder.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId);
        });

        modelBuilder.Entity<ScheduledPost>(builder =>
        {
            builder.ToTable("scheduled_posts");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TextContent).HasMaxLength(5000).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(x => x.FailureReason).HasMaxLength(500);
            builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId);
            builder.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId);
            builder.HasOne(x => x.Reservation).WithMany().HasForeignKey(x => x.ReservationId);
        });

        modelBuilder.Entity<PostTarget>(builder =>
        {
            builder.ToTable("post_targets");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Platform).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.ExternalAccountId).HasMaxLength(200).IsRequired();
            builder.HasOne(x => x.ScheduledPost)
                .WithMany(x => x.Targets)
                .HasForeignKey(x => x.ScheduledPostId);
        });

        modelBuilder.Entity<MediaAsset>(builder =>
        {
            builder.ToTable("media_assets");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.TenantId).HasColumnName("tenantid");
            builder.Property(x => x.UploadedByUserId).HasColumnName("uploadedbyuserid");
            builder.Property(x => x.OriginalFileName).HasColumnName("originalfilename").HasMaxLength(260).IsRequired();
            builder.Property(x => x.StoredFileName).HasColumnName("storedfilename").HasMaxLength(260).IsRequired();
            builder.Property(x => x.ContentType).HasColumnName("contenttype").HasMaxLength(120).IsRequired();
            builder.Property(x => x.SizeBytes).HasColumnName("sizebytes");
            builder.Property(x => x.PublicUrl).HasColumnName("publicurl").HasMaxLength(500).IsRequired();
            builder.Property(x => x.CreatedAtUtc).HasColumnName("createdatutc");
            builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId);
            builder.HasOne(x => x.UploadedByUser).WithMany().HasForeignKey(x => x.UploadedByUserId);
        });

        modelBuilder.Entity<ScheduledPostMedia>(builder =>
        {
            builder.ToTable("scheduled_post_media");
            builder.HasKey(x => new { x.ScheduledPostId, x.MediaAssetId });
            builder.Property(x => x.ScheduledPostId).HasColumnName("scheduledpostid");
            builder.Property(x => x.MediaAssetId).HasColumnName("mediaassetid");
            builder.HasOne(x => x.ScheduledPost)
                .WithMany(x => x.MediaLinks)
                .HasForeignKey(x => x.ScheduledPostId);
            builder.HasOne(x => x.MediaAsset)
                .WithMany(x => x.ScheduledPostLinks)
                .HasForeignKey(x => x.MediaAssetId);
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
