namespace Api.Media;

public interface IMediaStorage
{
    Task<(string StoredFileName, string PublicUrl)> SaveAsync(Guid tenantId, string originalFileName, Stream content, CancellationToken cancellationToken);
    Task<Stream?> GetFileStreamAsync(Guid tenantId, string storedFileName, CancellationToken cancellationToken);
}
