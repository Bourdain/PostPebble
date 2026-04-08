using Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure;

public sealed class MediaDbContext(DbContextOptions<MediaDbContext> options) : DbContext(options)
{
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
            builder.Ignore(x => x.Tenant);
            builder.Ignore(x => x.UploadedByUser);
            builder.Ignore(x => x.ScheduledPostLinks);
        });
    }
}

