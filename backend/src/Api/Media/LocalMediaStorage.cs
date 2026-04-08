namespace Api.Media;

public sealed class LocalMediaStorage(IWebHostEnvironment environment) : IMediaStorage
{
    public async Task<(string StoredFileName, string PublicUrl)> SaveAsync(
        Guid tenantId,
        string originalFileName,
        Stream content,
        CancellationToken cancellationToken)
    {
        var uploadsRoot = Path.Combine(environment.ContentRootPath, "uploads", tenantId.ToString());
        Directory.CreateDirectory(uploadsRoot);

        var extension = Path.GetExtension(originalFileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsRoot, storedFileName);

        await using (var fileStream = File.Create(fullPath))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        var publicUrl = $"/uploads/{tenantId}/{storedFileName}";
        return (storedFileName, publicUrl);
    }
}
