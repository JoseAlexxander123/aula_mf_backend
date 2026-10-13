using AulaVirtual.SharedKernel.Application.Storage;

namespace AulaVirtual.SharedKernel.Infrastructure.Storage;

public sealed class R2StorageOptions
{
    public const string SectionName = "Storage:R2";
    public bool Enabled { get; set; }
    public string? Endpoint { get; set; }
    public string? AccessKeyId { get; set; }
    public string? SecretAccessKey { get; set; }
    public string? PublicBucket { get; set; }
    public string? PrivateBucket { get; set; }
    public string? PublicBaseUrl { get; set; }
    public string[] AllowedContentTypes { get; set; } = [];
    public long MaxBackendUploadBytes { get; set; }
    public long MaxDirectUploadBytes { get; set; }
    public int MaxPresignedUrlSeconds { get; set; }

    public void Validate()
    {
        if (!Enabled) return;
        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(endpoint.Query) || !string.IsNullOrEmpty(endpoint.Fragment) ||
            string.IsNullOrWhiteSpace(AccessKeyId) || string.IsNullOrWhiteSpace(SecretAccessKey) ||
            string.IsNullOrWhiteSpace(PublicBucket) || string.IsNullOrWhiteSpace(PrivateBucket) ||
            PublicBucket == PrivateBucket ||
            (PublicBaseUrl is not null &&
                (!Uri.TryCreate(PublicBaseUrl, UriKind.Absolute, out var publicBase) ||
                 publicBase.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(publicBase.Query) ||
                 !string.IsNullOrEmpty(publicBase.Fragment))) ||
            AllowedContentTypes.Length == 0 || AllowedContentTypes.Any(string.IsNullOrWhiteSpace) ||
            MaxBackendUploadBytes <= 0 || MaxDirectUploadBytes <= 0 ||
            MaxPresignedUrlSeconds is < 1 or > 604800)
            throw new InvalidOperationException("Storage:R2 configuration is incomplete or invalid.");
    }

    public string Bucket(FileVisibility visibility) => visibility switch
    {
        FileVisibility.Public => PublicBucket!,
        FileVisibility.Private => PrivateBucket!,
        _ => throw new ArgumentOutOfRangeException(nameof(visibility))
    };

    public void ValidateContent(string contentType, long size, bool direct)
    {
        if (string.IsNullOrWhiteSpace(contentType) ||
            !AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Content type is not allowed.", nameof(contentType));
        if (size <= 0 || size > (direct ? MaxDirectUploadBytes : MaxBackendUploadBytes))
            throw new ArgumentOutOfRangeException(nameof(size), "File size exceeds configured limit.");
    }

    public void ValidateExpiration(TimeSpan expiresIn)
    {
        if (expiresIn <= TimeSpan.Zero || expiresIn.TotalSeconds > MaxPresignedUrlSeconds)
            throw new ArgumentOutOfRangeException(nameof(expiresIn));
    }
}
