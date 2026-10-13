namespace AulaVirtual.Modules.Courses.Application.Courses;

public sealed class CourseCoverOptions
{
    public const string SectionName = "Courses:Cover";
    public string[] AllowedContentTypes { get; set; } = ["image/jpeg", "image/png", "image/webp"];
    public long MaxBytes { get; set; } = 5 * 1024 * 1024;

    public void Validate()
    {
        if (MaxBytes <= 0 || AllowedContentTypes.Length == 0 ||
            AllowedContentTypes.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Courses:Cover configuration is invalid.");
    }

    public void ValidateFile(string contentType, long size)
    {
        if (string.IsNullOrWhiteSpace(contentType) ||
            !AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Cover content type is not allowed.", nameof(contentType));
        if (size <= 0 || size > MaxBytes)
            throw new ArgumentOutOfRangeException(nameof(size), "Cover size exceeds configured limit or is empty.");
    }
}
