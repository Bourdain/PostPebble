using Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure;

public sealed class SchedulerDbContext(DbContextOptions<SchedulerDbContext> options) : DbContext(options)
{
    public DbSet<ScheduledPost> ScheduledPosts => Set<ScheduledPost>();
    public DbSet<PostTarget> PostTargets => Set<PostTarget>();
    public DbSet<ScheduledPostMedia> ScheduledPostMediaLinks => Set<ScheduledPostMedia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScheduledPost>(builder =>
        {
            builder.ToTable("scheduled_posts");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TextContent).HasMaxLength(5000).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(x => x.FailureReason).HasMaxLength(500);
            builder.Ignore(x => x.Tenant);
            builder.Ignore(x => x.CreatedByUser);
            builder.Ignore(x => x.Reservation);
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

        modelBuilder.Entity<ScheduledPostMedia>(builder =>
        {
            builder.ToTable("scheduled_post_media");
            builder.HasKey(x => new { x.ScheduledPostId, x.MediaAssetId });
            builder.Property(x => x.ScheduledPostId).HasColumnName("scheduledpostid");
            builder.Property(x => x.MediaAssetId).HasColumnName("mediaassetid");
            builder.Ignore(x => x.MediaAsset);
            builder.HasOne(x => x.ScheduledPost)
                .WithMany(x => x.MediaLinks)
                .HasForeignKey(x => x.ScheduledPostId);
        });

    }
}

