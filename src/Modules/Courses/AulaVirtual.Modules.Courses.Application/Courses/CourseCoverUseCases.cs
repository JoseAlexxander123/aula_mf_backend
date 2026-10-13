using AulaVirtual.Modules.Courses.Domain.Courses;
using AulaVirtual.SharedKernel.Application.Storage;
using Microsoft.Extensions.Logging;

namespace AulaVirtual.Modules.Courses.Application.Courses;

public sealed class CourseCoverUseCases(ICourseRepository repository, IFileStorage storage,
    CourseCoverOptions coverOptions, TimeProvider timeProvider, ILogger<CourseCoverUseCases> logger)
{
    public async Task<CourseDetails?> ReplaceCourseCoverAsync(Guid id, Stream content,
        string contentType, long size, string? originalFileName, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) throw new ArgumentException("Course identifier cannot be empty.", nameof(id));
        ArgumentNullException.ThrowIfNull(content);
        coverOptions.ValidateFile(contentType, size);
        if (!content.CanRead) throw new ArgumentException("Cover stream must be readable.", nameof(content));
        if (originalFileName is { Length: > 255 } || originalFileName?.Any(char.IsControl) == true)
            throw new ArgumentException("Original filename contains invalid metadata characters.", nameof(originalFileName));
        var course = await repository.GetAsync(id, cancellationToken);
        if (course is null) return null;
        var previousKey = course.CoverObjectKey;
        var previousContentType = course.CoverContentType;
        var previousSize = course.CoverSize;
        var previousFileName = course.CoverOriginalFileName;
        var previousUpdatedAt = course.UpdatedAt;
        var newKey = FileObjectKeys.CourseCover(id, Guid.NewGuid());
        var stored = await storage.StoreAsync(new StoreFileRequest(newKey, FileVisibility.Public,
            content, contentType, size, originalFileName), cancellationToken);
        try
        {
            course.ReplaceCover(stored.ObjectKey, stored.ContentType, stored.Size ?? size,
                originalFileName, timeProvider.GetUtcNow());
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            course.RestoreCoverAfterFailedSave(previousKey, previousContentType, previousSize,
                previousFileName, previousUpdatedAt);
            await TryDeleteAsync(newKey, "new", cancellationToken);
            throw;
        }
        if (previousKey is not null) await TryDeleteAsync(previousKey, "previous", cancellationToken);
        return CourseDetails.From(course);
    }

    private async Task TryDeleteAsync(string key, string role, CancellationToken cancellationToken)
    {
        try { await storage.DeleteAsync(key, FileVisibility.Public, cancellationToken); }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not clean up {Role} course cover {ObjectKey}", role, key);
        }
    }
}
