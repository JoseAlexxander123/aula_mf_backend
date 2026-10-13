using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using AulaVirtual.Modules.Courses.Application.Courses;
using AulaVirtual.Modules.Courses.Domain.Courses;
using AulaVirtual.Modules.Courses.Domain.Offerings;
using AulaVirtual.Modules.Courses.Infrastructure.Persistence;
using AulaVirtual.SharedKernel.Application.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AulaVirtual.Modules.Courses.Tests;

public sealed class PublicCourseCatalogTests
{
    private const string Route = "/api/v1/public/courses";
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task EmptyCatalog_ReturnsAnonymous200Array()
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task VisibilityAndPublicContract_KeepOnlyPublishedCoursesAndOfferings()
    {
        await using var factory = new Factory();
        var category = factory.AddCategory("Kids Science");
        var draftCourse = factory.AddCourse(category, published: false);
        factory.AddOffering(draftCourse, published: true);
        var noOfferings = factory.AddCourse(category, published: true);
        var draftOnly = factory.AddCourse(category, published: true);
        factory.AddOffering(draftOnly, published: false);
        var visible = factory.AddCourse(category, published: true, cover: true,
            title: "Astronomy", description: "Explore the sky");
        var first = factory.AddOffering(visible, published: true, name: "Weekend", price: 120);
        var second = factory.AddOffering(visible, published: true, name: "Evening", price: 150);
        var hidden = factory.AddOffering(visible, published: false, name: "Hidden edition");
        var withoutCover = factory.AddCourse(category, published: true, title: "Biology");
        factory.AddOffering(withoutCover, published: true);

        using var client = factory.CreateClient();
        using var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(body);
        var courses = body.RootElement.EnumerateArray().ToArray();
        Assert.Equal(2, courses.Length);
        Assert.DoesNotContain(courses, item => item.GetProperty("courseId").GetGuid() == draftCourse.Id);
        Assert.DoesNotContain(courses, item => item.GetProperty("courseId").GetGuid() == noOfferings.Id);
        Assert.DoesNotContain(courses, item => item.GetProperty("courseId").GetGuid() == draftOnly.Id);
        var astronomy = courses.Single(item => item.GetProperty("courseId").GetGuid() == visible.Id);
        Assert.Equal("Astronomy", astronomy.GetProperty("name").GetString());
        Assert.Equal("Explore the sky", astronomy.GetProperty("shortDescription").GetString());
        Assert.Equal(category.Id, astronomy.GetProperty("category").GetProperty("id").GetGuid());
        Assert.Equal("Kids Science", astronomy.GetProperty("category").GetProperty("name").GetString());
        Assert.Equal($"https://cdn.example.test/{visible.CoverObjectKey}",
            astronomy.GetProperty("coverUrl").GetString());
        var offerings = astronomy.GetProperty("offerings").EnumerateArray().ToArray();
        Assert.Equal(2, offerings.Length);
        Assert.Equal(new[] { first.Id, second.Id }.Order(),
            offerings.Select(item => item.GetProperty("offeringId").GetGuid()).Order());
        Assert.DoesNotContain(offerings, item => item.GetProperty("offeringId").GetGuid() == hidden.Id);
        var weekend = offerings.Single(item => item.GetProperty("offeringId").GetGuid() == first.Id);
        Assert.Equal("Weekend", weekend.GetProperty("name").GetString());
        Assert.Equal(120, weekend.GetProperty("basePrice").GetDecimal());
        Assert.Equal("PEN", weekend.GetProperty("currencyCode").GetString());
        Assert.Equal(JsonValueKind.Null, weekend.GetProperty("promotionalPrice").ValueKind);
        Assert.Equal(JsonValueKind.Null, courses.Single(item =>
            item.GetProperty("courseId").GetGuid() == withoutCover.Id)
            .GetProperty("coverUrl").ValueKind);
        Assert.Equal(1, factory.Storage.PublicUrlCalls);

        Assert.Equal(new[] { "courseId", "name", "shortDescription", "category", "coverUrl", "offerings" },
            astronomy.EnumerateObject().Select(property => property.Name));
        Assert.Equal(new[] { "offeringId", "name", "basePrice", "currencyCode", "promotionalPrice" },
            offerings[0].EnumerateObject().Select(property => property.Name));
        foreach (var forbidden in new[]
        {
            "maximumCapacity", "sessions", "coverObjectKey", "instructorIds",
            "plannedSessionCount", "createdAt", "updatedAt"
        })
            Assert.DoesNotContain(forbidden, body.RootElement.ToString(),
                StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SpecificPromotions_UseCurrentWindowAndExistingPriceRules()
    {
        await using var factory = new Factory();
        var category = factory.AddCategory("Corporate");
        var course = factory.AddCourse(category, published: true);
        var percentage = factory.AddOffering(course, published: true, price: 200);
        percentage.SetSpecificPromotion(SpecificPromotionType.Percentage, 25,
            Now.AddDays(-1), Now.AddDays(1), Now);
        var fixedAmount = factory.AddOffering(course, published: true, price: 100);
        fixedAmount.SetSpecificPromotion(SpecificPromotionType.FixedAmount, 150,
            Now.AddDays(-1), Now.AddDays(1), Now);
        var future = factory.AddOffering(course, published: true, price: 80);
        future.SetSpecificPromotion(SpecificPromotionType.Percentage, 10,
            Now.AddDays(1), Now.AddDays(2), Now);
        var expired = factory.AddOffering(course, published: true, price: 90);
        expired.SetSpecificPromotion(SpecificPromotionType.Percentage, 10,
            Now.AddDays(-2), Now, Now);

        using var client = factory.CreateClient();
        var courses = await client.GetFromJsonAsync<JsonDocument>(Route);
        Assert.NotNull(courses);
        var offerings = courses.RootElement[0].GetProperty("offerings").EnumerateArray()
            .ToDictionary(item => item.GetProperty("offeringId").GetGuid());
        Assert.Equal(150, offerings[percentage.Id].GetProperty("promotionalPrice").GetDecimal());
        Assert.Equal(0, offerings[fixedAmount.Id].GetProperty("promotionalPrice").GetDecimal());
        Assert.Equal(JsonValueKind.Null, offerings[future.Id].GetProperty("promotionalPrice").ValueKind);
        Assert.Equal(JsonValueKind.Null, offerings[expired.Id].GetProperty("promotionalPrice").ValueKind);
    }

    [Fact]
    public async Task Catalog_UsesExistingPublicRateLimitAndProblemDetails()
    {
        await using var factory = new Factory(permitLimit: 2);
        using var client = factory.CreateClient();

        using var first = await client.GetAsync(Route);
        using var second = await client.GetAsync(Route);
        using var rejected = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public void EfQuery_TranslatesPublishedFilterAndProjectsOnlyCatalogColumns()
    {
        var options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseNpgsql("Host=localhost;Database=not_used;Username=not_used")
            .Options;
        using var db = new CoursesDbContext(options);
        var sql = PublicCourseCatalogRepository.BuildQuery(db.Courses, db.Categories,
            db.CourseOfferings).ToQueryString();

        Assert.Contains("is_published", sql);
        Assert.Contains("currency_code", sql);
        Assert.Contains("published", sql);
        Assert.DoesNotContain("maximum_capacity", sql);
        Assert.DoesNotContain("planned_session_count", sql);
        Assert.DoesNotContain("offering_sessions", sql);
    }

    private sealed class Factory(int permitLimit = 60) : WebApplicationFactory<Program>
    {
        private readonly string signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        private readonly List<Course> courses = [];
        private readonly List<CourseCategory> categories = [];
        private readonly List<CourseOffering> offerings = [];
        public FakeStorage Storage { get; } = new();

        public CourseCategory AddCategory(string name)
        {
            var category = CourseCategory.Create(Guid.NewGuid(), name, Now);
            categories.Add(category);
            return category;
        }

        public Course AddCourse(CourseCategory category, bool published, bool cover = false,
            string title = "Course", string? description = null)
        {
            var course = Course.Create(Guid.NewGuid(), title, category.Id, Now);
            course.Update(title, category.Id, description, null, null, null, null, null, Now);
            if (cover) course.ReplaceCover($"courses/{course.Id:N}/covers/cover.png",
                "image/png", 10, "cover.png", Now);
            if (published) course.Publish(Now);
            courses.Add(course);
            return course;
        }

        public CourseOffering AddOffering(Course course, bool published, string name = "Edition",
            decimal price = 100)
        {
            var offering = CourseOffering.Create(Guid.NewGuid(), course.Id, name, 20,
                price, 1, "PEN", [new SessionPlan(null, Now.AddDays(1), Now.AddDays(1).AddHours(1),
                    "https://private.example.test/class")], Now);
            if (published)
            {
                offering.AssignInstructors([Guid.NewGuid()], Now);
                offering.Publish(Now);
            }
            offerings.Add(offering);
            return offering;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development")
                .UseSetting("Authentication:Jwt:SigningKey", signingKey)
                .UseSetting("PublicApi:RateLimit:PermitLimit", permitLimit.ToString())
                .UseSetting("PublicApi:RateLimit:WindowSeconds", "60")
                .UseSetting("Storage:R2:Enabled", "false");
            builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Host=localhost;Database=not_used;Username=not_used"
                }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPublicCourseCatalogRepository>();
                services.AddSingleton<IPublicCourseCatalogRepository>(new FakeRepository(
                    courses, categories, offerings));
                services.AddSingleton<TimeProvider>(new FixedTimeProvider());
                services.AddSingleton<IFileStorage>(Storage);
            });
        }
    }

    private sealed class FakeRepository(List<Course> courses, List<CourseCategory> categories,
        List<CourseOffering> offerings) : IPublicCourseCatalogRepository
    {
        public Task<IReadOnlyList<PublicCourseCatalogRow>> ListAsync(CancellationToken token) =>
            Task.FromResult<IReadOnlyList<PublicCourseCatalogRow>>(
                PublicCourseCatalogRepository.BuildQuery(courses.AsQueryable(), categories.AsQueryable(),
                    offerings.AsQueryable()).ToArray());
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class FakeStorage : IFileStorage
    {
        public int PublicUrlCalls { get; private set; }
        public Uri? GetPublicUrl(string key)
        {
            PublicUrlCalls++;
            return new Uri($"https://cdn.example.test/{key}");
        }
        public Task<StoredObject> StoreAsync(StoreFileRequest request, CancellationToken token = default) =>
            throw new NotSupportedException();
        public Task DeleteAsync(string key, FileVisibility visibility, CancellationToken token = default) =>
            throw new NotSupportedException();
        public Task<FileObjectInfo?> GetInfoAsync(string key, FileVisibility visibility,
            CancellationToken token = default) => throw new NotSupportedException();
        public Task<Uri> GetPrivateDownloadUrlAsync(string key, TimeSpan expiry,
            CancellationToken token = default) => throw new NotSupportedException();
        public Task<DirectUploadGrant> CreateDirectUploadAsync(DirectUploadRequest request,
            CancellationToken token = default) => throw new NotSupportedException();
    }
}
