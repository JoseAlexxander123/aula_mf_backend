namespace AulaVirtual.SharedKernel.Application.Storage;

public enum FileVisibility { Public, Private }

public sealed record StoredObject(string ObjectKey, FileVisibility Visibility, string ContentType, long? Size);
public sealed record StoreFileRequest(string ObjectKey, FileVisibility Visibility, Stream Content,
    string ContentType, long? Size = null, string? OriginalFileName = null);
public sealed record DirectUploadRequest(string ObjectKey, FileVisibility Visibility,
    string ContentType, long Size, TimeSpan ExpiresIn);
public sealed record DirectUploadGrant(Uri Url, string Method, IReadOnlyDictionary<string, string> Headers,
    DateTimeOffset ExpiresAt);
public sealed record FileObjectInfo(string ObjectKey, FileVisibility Visibility, string ContentType,
    long Size, DateTimeOffset? LastModified);

/// <summary>Callers authorize access before requesting a private download URL or direct upload grant.</summary>
public interface IFileStorage
{
    Task<StoredObject> StoreAsync(StoreFileRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(string objectKey, FileVisibility visibility, CancellationToken cancellationToken = default);
    Task<FileObjectInfo?> GetInfoAsync(string objectKey, FileVisibility visibility, CancellationToken cancellationToken = default);
    Uri? GetPublicUrl(string objectKey);
    Task<Uri> GetPrivateDownloadUrlAsync(string objectKey, TimeSpan expiresIn,
        CancellationToken cancellationToken = default);
    Task<DirectUploadGrant> CreateDirectUploadAsync(DirectUploadRequest request,
        CancellationToken cancellationToken = default);
}
