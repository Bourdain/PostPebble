using Api.Billing;
using Api.Domain;
using Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Api.Media;

public static class MediaEndpoints
{
    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/media").RequireAuthorization();
        group.MapPost("/upload", UploadAsync);
        group.MapGet("/{tenantId:guid}", ListAsync);
        return app;
    }

    private static async Task<IResult> UploadAsync(
        HttpRequest request,
        ClaimsPrincipal principal,
        MediaDbContext dbContext,
        ITenantAccessService tenantAccessService,
        IMediaStorage mediaStorage,
        IOptions<MediaStorageOptions> optionsAccessor,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserProvider.GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (!request.HasFormContentType)
        {
            return Results.BadRequest("Expected multipart/form-data.");
        }

        var form = await request.ReadFormAsync(cancellationToken);
        if (!Guid.TryParse(form["tenantId"], out var tenantId))
        {
            return Results.BadRequest("tenantId is required.");
        }

        var file = form.Files["file"] ?? form.Files.FirstOrDefault();
        if (file is null || file.Length <= 0)
        {
            return Results.BadRequest("No file uploaded.");
        }
        var options = optionsAccessor.Value;
        if (file.Length > options.MaxFileSizeBytes)
        {
            return Results.BadRequest($"File too large. Max size is {options.MaxFileSizeBytes} bytes.");
        }
        if (!options.AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return Results.BadRequest("Unsupported media type.");
        }

        var isMember = await tenantAccessService.IsTenantMemberAsync(userId.Value, tenantId, cancellationToken);
        if (!isMember)
        {
            return Results.Forbid();
        }

        string storedFileName;
        string publicUrl;
        await using (var stream = file.OpenReadStream())
        {
            (storedFileName, publicUrl) = await mediaStorage.SaveAsync(tenantId, file.FileName, stream, cancellationToken);
        }
        var asset = new MediaAsset
        {
            TenantId = tenantId,
            UploadedByUserId = userId.Value,
            OriginalFileName = file.FileName,
            StoredFileName = storedFileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            PublicUrl = publicUrl
        };

        dbContext.MediaAssets.Add(asset);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new
        {
            asset.Id,
            asset.OriginalFileName,
            asset.ContentType,
            asset.SizeBytes,
            asset.PublicUrl
        });
    }

    private static async Task<IResult> ListAsync(
        Guid tenantId,
        ClaimsPrincipal principal,
        MediaDbContext dbContext,
        ITenantAccessService tenantAccessService,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserProvider.GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var isMember = await tenantAccessService.IsTenantMemberAsync(userId.Value, tenantId, cancellationToken);
        if (!isMember)
        {
            return Results.Forbid();
        }

        var rows = await dbContext.MediaAssets
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(100)
            .Select(x => new
            {
                x.Id,
                x.OriginalFileName,
                x.ContentType,
                x.SizeBytes,
                x.PublicUrl,
                x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(rows);
    }
}
