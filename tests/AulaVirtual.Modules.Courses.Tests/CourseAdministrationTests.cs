using AulaVirtual.Modules.Courses.Application.Courses;
using AulaVirtual.Modules.Courses.Api;
using AulaVirtual.Modules.Courses.Api.Controllers;
using AulaVirtual.Modules.Courses.Api.Contracts;
using AulaVirtual.Modules.Courses.Domain.Courses;
using AulaVirtual.SharedKernel.Application.Storage;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AulaVirtual.Modules.Courses.Tests;

public sealed class CourseAdministrationTests
{
    private readonly FakeRepository repository = new();
    private readonly FakeStorage storage = new();
    private readonly CourseCoverOptions options = new();

    private CourseUseCases UseCases() => new(repository, new SaveCourseValidator(), TimeProvider.System);
    private CourseCoverUseCases CoverUseCases() => new(repository, storage, options,
        TimeProvider.System, NullLogger<CourseCoverUseCases>.Instance);

    private static SaveCourseCommand Command(Guid? categoryId = null, string title = "Course") =>
        new(categoryId, title, "Summary", "Description", "Objectives", "Syllabus", "Audience", "Prerequisites");

    [Fact]
    public void ListControllersResolveWithoutFileStorage()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=unused;Username=unused"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoursesModule(configuration);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.Null(scope.ServiceProvider.GetService<IFileStorage>());
        Assert.Null(scope.ServiceProvider.GetService<CourseCoverUseCases>());
        Assert.NotNull(ActivatorUtilities.CreateInstance<CoursesController>(scope.ServiceProvider));
        Assert.NotNull(ActivatorUtilities.CreateInstance<CourseCategoriesController>(scope.ServiceProvider));
    }

    [Fact]
    public async Task CoverUploadReturnsServiceUnavailableWithoutStorage()
    {
        var controller = new CoursesController(UseCases());
        var result = await controller.ReplaceCover(Guid.NewGuid(), null!, null, default);
        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, problem.StatusCode);
    }

    [Fact]
    public async Task HostStartsWithStorageDisabled()
    {
        await using var factory = new StorageDisabledFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed class StorageDisabledFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development")
                .UseSetting("Storage:R2:Enabled", "false")
                .UseSetting("Authentication:Jwt:SigningKey",
                    Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
            builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Host=localhost;Database=not_used;Username=not_used"
                }));
        }
    }

    [Fact]
    public async Task CreateUpdateGetList_KeepAcademicFieldsAndDraftState()
    {
        var service = UseCases();
        var created = await service.CreateCourseAsync(Command(), default);
        Assert.False(created.IsPublished);
        Assert.Equal("Summary", created.ShortDescription);
        Assert.Null(created.CoverObjectKey);
        var updated = await service.UpdateCourseAsync(created.Id, Command(title: "Updated"), default);
        Assert.Equal("Updated", updated!.Title);
        Assert.Equal("Description", (await service.GetAdminCourseByIdAsync(created.Id, default))!.FullDescription);
        Assert.Single(await service.ListAdminCoursesAsync(default));
        Assert.Null(await service.GetAdminCourseByIdAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task CourseResponsesIncludePublicCoverUrlInListDetailAndReplacement()
    {
        var service = UseCases();
        var controller = new CoursesController(service, storage);
        var course = await service.CreateCourseAsync(Command(), default);
        var withoutCover = Assert.IsType<CourseAdminResponse>(
            Assert.IsType<OkObjectResult>(await controller.Get(course.Id, default)).Value);
        Assert.Null(withoutCover.CoverUrl);
        Assert.Equal(0, storage.PublicUrlCalls);

        using var firstContent = new MemoryStream([1, 2, 3]);
        var firstFile = new FormFile(firstContent, 0, 3, "file", "first.png")
        {
            Headers = new HeaderDictionary(), ContentType = "image/png"
        };
        var first = Assert.IsType<CourseAdminResponse>(Assert.IsType<OkObjectResult>(
            await controller.ReplaceCover(course.Id, firstFile, CoverUseCases(), default)).Value);
        Assert.Equal($"https://cdn.example.test/{first.CoverObjectKey}", first.CoverUrl);
        Assert.Equal("first.png", first.CoverOriginalFileName);
        Assert.Equal("image/png", first.CoverContentType);
        Assert.Equal(3, first.CoverSize);

        var listed = Assert.Single(Assert.IsAssignableFrom<IEnumerable<CourseAdminResponse>>(
            Assert.IsType<OkObjectResult>(await controller.List(default)).Value));
        var detailed = Assert.IsType<CourseAdminResponse>(
            Assert.IsType<OkObjectResult>(await controller.Get(course.Id, default)).Value);
        Assert.Equal(first.CoverUrl, listed.CoverUrl);
        Assert.Equal(first.CoverUrl, detailed.CoverUrl);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(detailed, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        Assert.Equal(first.CoverUrl, json.RootElement.GetProperty("coverUrl").GetString());

        using var secondContent = new MemoryStream([4, 5]);
        var secondFile = new FormFile(secondContent, 0, 2, "file", "second.png")
        {
            Headers = new HeaderDictionary(), ContentType = "image/png"
        };
        var second = Assert.IsType<CourseAdminResponse>(Assert.IsType<OkObjectResult>(
            await controller.ReplaceCover(course.Id, secondFile, CoverUseCases(), default)).Value);
        Assert.NotEqual(first.CoverUrl, second.CoverUrl);
        Assert.Equal($"https://cdn.example.test/{second.CoverObjectKey}", second.CoverUrl);
        Assert.Equal(second.CoverUrl, Assert.IsType<CourseAdminResponse>(
            Assert.IsType<OkObjectResult>(await controller.Get(course.Id, default)).Value).CoverUrl);
        Assert.Contains(first.CoverObjectKey!, storage.Deleted);
    }

    [Fact]
    public async Task CourseWithCoverWithoutPublicBaseUrlDoesNotInventUrl()
    {
        var service = UseCases();
        var course = await service.CreateCourseAsync(Command(), default);
        using var content = new MemoryStream([1]);
        await CoverUseCases().ReplaceCourseCoverAsync(course.Id, content, "image/png", 1, "cover.png", default);
        storage.PublicBaseUrl = null;
        var controller = new CoursesController(service, storage);

        var detailed = Assert.IsType<CourseAdminResponse>(
            Assert.IsType<OkObjectResult>(await controller.Get(course.Id, default)).Value);
        var listed = Assert.Single(Assert.IsAssignableFrom<IEnumerable<CourseAdminResponse>>(
            Assert.IsType<OkObjectResult>(await controller.List(default)).Value));
        Assert.Null(detailed.CoverUrl);
        Assert.Null(listed.CoverUrl);
        Assert.NotNull(detailed.CoverObjectKey);
        Assert.Equal("cover.png", detailed.CoverOriginalFileName);
    }

    [Fact]
    public async Task CategoriesAreCheckedAndPublicationRequiresExistingCategory()
    {
        var service = UseCases();
        var badId = Guid.NewGuid();
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCourseAsync(Command(badId), default));
        await Assert.ThrowsAsync<ValidationException>(() => service.CreateCourseAsync(Command(title: " "), default));
        var course = await service.CreateCourseAsync(Command(), default);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetPublicationAsync(course.Id, true, default));
        var category = CourseCategory.Create(Guid.NewGuid(), "Science", DateTimeOffset.UtcNow);
        repository.Categories.Add(category);
        Assert.Single(await service.ListCourseCategoriesAsync(default));
        await service.UpdateCourseAsync(course.Id, Command(category.Id), default);
        Assert.True((await service.SetPublicationAsync(course.Id, true, default))!.IsPublished);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateCourseAsync(course.Id, Command(), default));
        Assert.False((await service.SetPublicationAsync(course.Id, false, default))!.IsPublished);
    }

    [Fact]
    public async Task CategoryCreateGetUpdateAndDelete_PreserveIdentityAndTimestamps()
    {
        var service = UseCases();
        var created = await service.CreateCourseCategoryAsync("  Science  ", default);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Science", created.Name);
        Assert.Equal(created, await service.GetCourseCategoryAsync(created.Id, default));
        Assert.Equal(created, Assert.Single(await service.ListCourseCategoriesAsync(default)));

        var entity = Assert.Single(repository.Categories);
        var createdAt = entity.CreatedAt;
        Assert.Null(entity.UpdatedAt);
        var updated = await service.UpdateCourseCategoryAsync(created.Id, "  Mathematics  ", default);
        Assert.Equal("Mathematics", updated!.Name);
        Assert.Equal(createdAt, entity.CreatedAt);
        Assert.NotNull(entity.UpdatedAt);
        Assert.Equal(TimeSpan.Zero, entity.UpdatedAt.Value.Offset);
        Assert.Equal(CourseCategoryDeletion.Deleted,
            await service.DeleteCourseCategoryAsync(created.Id, default));
        Assert.Empty(repository.Categories);
    }

    [Fact]
    public async Task CategoryNamesAndIdentifiersAreValidated()
    {
        var service = UseCases();
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCourseCategoryAsync("  ", default));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.CreateCourseCategoryAsync(new string('x', 151), default));
        var category = await service.CreateCourseCategoryAsync("Valid", default);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateCourseCategoryAsync(category.Id, "  ", default));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.UpdateCourseCategoryAsync(category.Id, new string('x', 151), default));
        Assert.Equal("Valid", repository.Categories.Single().Name);
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetCourseCategoryAsync(Guid.Empty, default));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateCourseCategoryAsync(Guid.Empty, "Name", default));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.DeleteCourseCategoryAsync(Guid.Empty, default));
    }

    [Fact]
    public async Task CategoryMissingAndReferencedDelete_AreControlled()
    {
        var service = UseCases();
        var missing = Guid.NewGuid();
        Assert.Null(await service.GetCourseCategoryAsync(missing, default));
        Assert.Null(await service.UpdateCourseCategoryAsync(missing, "Name", default));
        Assert.Equal(CourseCategoryDeletion.NotFound,
            await service.DeleteCourseCategoryAsync(missing, default));

        var category = await service.CreateCourseCategoryAsync("Used", default);
        var course = await service.CreateCourseAsync(Command(category.Id), default);
        Assert.Equal(CourseCategoryDeletion.InUse,
            await service.DeleteCourseCategoryAsync(category.Id, default));
        Assert.Single(repository.Categories);
        Assert.Equal(category.Id, course.CategoryId);

        repository.Courses.Clear();
        repository.FailDeleteWithInUse = true;
        Assert.Equal(CourseCategoryDeletion.InUse,
            await service.DeleteCourseCategoryAsync(category.Id, default));
        Assert.Single(repository.Categories);
    }

    [Fact]
    public async Task ValidCoverUsesServerKeyAndPublicVisibility()
    {
        var service = UseCases();
        var coverService = CoverUseCases();
        var course = await service.CreateCourseAsync(Command(), default);
        using var stream = new MemoryStream([1, 2, 3]);
        var result = await coverService.ReplaceCourseCoverAsync(course.Id, stream, "image/png", 3, "cover.png", default);
        Assert.StartsWith($"courses/{course.Id:N}/covers/", result!.CoverObjectKey);
        Assert.Equal(FileVisibility.Public, storage.LastStore!.Visibility);
        Assert.Equal(result.CoverObjectKey, storage.LastStore.ObjectKey);
        Assert.Equal(3, result.CoverSize);
        Assert.Equal("cover.png", result.CoverOriginalFileName);
        Assert.DoesNotContain(typeof(Course).GetProperties(), p => p.PropertyType == typeof(byte[]));
    }

    [Fact]
    public async Task CoverValidationRejectsMimeEmptyAndOversizeBeforeStorage()
    {
        var service = UseCases();
        var coverService = CoverUseCases();
        var course = await service.CreateCourseAsync(Command(), default);
        using var stream = new MemoryStream([1]);
        await Assert.ThrowsAsync<ArgumentException>(() => coverService.ReplaceCourseCoverAsync(
            course.Id, stream, "application/pdf", 1, "cover.pdf", default));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => coverService.ReplaceCourseCoverAsync(
            course.Id, stream, "image/png", 0, "cover.png", default));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => coverService.ReplaceCourseCoverAsync(
            course.Id, stream, "image/png", options.MaxBytes + 1, "cover.png", default));
        Assert.Null(storage.LastStore);
    }

    [Fact]
    public async Task ReplacementStoresThenPersistsThenDeletesPrevious()
    {
        var service = UseCases();
        var coverService = CoverUseCases();
        var course = await service.CreateCourseAsync(Command(), default);
        using var stream = new MemoryStream([1]);
        var first = await coverService.ReplaceCourseCoverAsync(course.Id, stream, "image/png", 1, null, default);
        var second = await coverService.ReplaceCourseCoverAsync(course.Id, stream, "image/png", 1, null, default);
        Assert.NotEqual(first!.CoverObjectKey, second!.CoverObjectKey);
        Assert.Contains(first.CoverObjectKey!, storage.Deleted);
        Assert.Equal(second.CoverObjectKey, repository.Courses.Single().CoverObjectKey);
    }

    [Fact]
    public async Task UploadFailurePreservesOldCoverAndCleanupFailureKeepsNewCover()
    {
        var service = UseCases();
        var coverService = CoverUseCases();
        var course = await service.CreateCourseAsync(Command(), default);
        using var stream = new MemoryStream([1]);
        var first = await coverService.ReplaceCourseCoverAsync(course.Id, stream, "image/png", 1, null, default);
        storage.FailStore = true;
        await Assert.ThrowsAsync<IOException>(() => coverService.ReplaceCourseCoverAsync(
            course.Id, stream, "image/png", 1, null, default));
        Assert.Equal(first!.CoverObjectKey, repository.Courses.Single().CoverObjectKey);
        storage.FailStore = false;
        storage.FailDelete = true;
        var second = await coverService.ReplaceCourseCoverAsync(course.Id, stream, "image/png", 1, null, default);
        Assert.NotEqual(first.CoverObjectKey, second!.CoverObjectKey);
        Assert.Equal(second.CoverObjectKey, repository.Courses.Single().CoverObjectKey);
    }

    [Fact]
    public async Task SaveFailureAttemptsNewObjectCleanup()
    {
        var service = UseCases();
        var coverService = CoverUseCases();
        var course = await service.CreateCourseAsync(Command(), default);
        repository.FailSave = true;
        using var stream = new MemoryStream([1]);
        await Assert.ThrowsAsync<IOException>(() => coverService.ReplaceCourseCoverAsync(
            course.Id, stream, "image/png", 1, null, default));
        Assert.Contains(storage.LastStore!.ObjectKey, storage.Deleted);
        Assert.Null(repository.Courses.Single().CoverObjectKey);
    }

    private sealed class FakeRepository : ICourseRepository
    {
        public List<Course> Courses { get; } = [];
        public List<CourseCategory> Categories { get; } = [];
        public bool FailSave { get; set; }
        public bool FailDeleteWithInUse { get; set; }
        public Task<Course?> GetAsync(Guid id, CancellationToken token) => Task.FromResult(Courses.SingleOrDefault(c => c.Id == id));
        public Task<IReadOnlyList<Course>> ListAsync(CancellationToken token) => Task.FromResult<IReadOnlyList<Course>>(Courses);
        public Task<bool> CategoryExistsAsync(Guid id, CancellationToken token) => Task.FromResult(Categories.Any(c => c.Id == id));
        public Task<CourseCategory?> GetCategoryAsync(Guid id, CancellationToken token) =>
            Task.FromResult(Categories.SingleOrDefault(c => c.Id == id));
        public Task<IReadOnlyList<CourseCategory>> ListCategoriesAsync(CancellationToken token) =>
            Task.FromResult<IReadOnlyList<CourseCategory>>(Categories);
        public Task<bool> CategoryHasCoursesAsync(Guid id, CancellationToken token) =>
            Task.FromResult(Courses.Any(c => c.CategoryId == id));
        public void AddCategory(CourseCategory category) => Categories.Add(category);
        public Task<bool> DeleteCategoryAsync(CourseCategory category, CancellationToken token) =>
            FailDeleteWithInUse
                ? Task.FromException<bool>(new CourseCategoryInUseException())
                : Task.FromResult(Categories.Remove(category));
        public void Add(Course course) => Courses.Add(course);
        public Task SaveChangesAsync(CancellationToken token) => FailSave ? Task.FromException(new IOException("save failed")) : Task.CompletedTask;
    }

    private sealed class FakeStorage : IFileStorage
    {
        public string? PublicBaseUrl { get; set; } = "https://cdn.example.test";
        public int PublicUrlCalls { get; private set; }
        public StoreFileRequest? LastStore { get; private set; }
        public List<string> Deleted { get; } = [];
        public bool FailStore { get; set; }
        public bool FailDelete { get; set; }
        public Task<StoredObject> StoreAsync(StoreFileRequest request, CancellationToken token = default)
        {
            if (FailStore) throw new IOException("upload failed");
            LastStore = request;
            return Task.FromResult(new StoredObject(request.ObjectKey, request.Visibility, request.ContentType, request.Size));
        }
        public Task DeleteAsync(string key, FileVisibility visibility, CancellationToken token = default)
        {
            if (FailDelete) throw new IOException("cleanup failed");
            Deleted.Add(key);
            return Task.CompletedTask;
        }
        public Task<FileObjectInfo?> GetInfoAsync(string key, FileVisibility visibility, CancellationToken token = default) =>
            Task.FromResult<FileObjectInfo?>(null);
        public Uri? GetPublicUrl(string key)
        {
            PublicUrlCalls++;
            return PublicBaseUrl is null ? null : new Uri($"{PublicBaseUrl}/{key}");
        }
        public Task<Uri> GetPrivateDownloadUrlAsync(string key, TimeSpan expiry, CancellationToken token = default) =>
            throw new NotSupportedException();
        public Task<DirectUploadGrant> CreateDirectUploadAsync(DirectUploadRequest request, CancellationToken token = default) =>
            throw new NotSupportedException();
    }
}
