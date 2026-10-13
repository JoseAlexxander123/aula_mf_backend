using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using AulaVirtual.Modules.Authorization.Application;
using AulaVirtual.Modules.Courses.Application.Courses;
using AulaVirtual.Modules.Courses.Domain.Courses;
using AulaVirtual.SharedKernel.Application.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace AulaVirtual.Modules.Courses.Tests;

public sealed class CourseCategoryApiTests
{
    [Fact]
    public async Task CategoryCrud_UsesCoursesManageAndReturnsExpectedHttpContracts()
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        const string route = "/api/v1/course-categories";

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync(route, new { name = "Science" })).StatusCode);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", factory.Token(factory.NormalUserId));
        foreach (var request in new[]
        {
            new HttpRequestMessage(HttpMethod.Get, route),
            new HttpRequestMessage(HttpMethod.Get, $"{route}/{Guid.NewGuid()}"),
            new HttpRequestMessage(HttpMethod.Post, route) { Content = JsonContent.Create(new { name = "Science" }) },
            new HttpRequestMessage(HttpMethod.Put, $"{route}/{Guid.NewGuid()}")
                { Content = JsonContent.Create(new { name = "Science" }) },
            new HttpRequestMessage(HttpMethod.Delete, $"{route}/{Guid.NewGuid()}")
        })
        {
            using (request)
            using (var response = await client.SendAsync(request))
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", factory.Token(factory.ManagerUserId));
        Assert.Empty((await client.GetFromJsonAsync<CategoryBody[]>(route))!);

        using var createdResponse = await client.PostAsJsonAsync(route, new { name = "  Science  " });
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<CategoryBody>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Science", created.Name);
        Assert.Equal($"{route}/{created.Id}", createdResponse.Headers.Location?.AbsolutePath);
        Assert.Equal(created, Assert.Single((await client.GetFromJsonAsync<CategoryBody[]>(route))!));
        Assert.Equal(created, await client.GetFromJsonAsync<CategoryBody>($"{route}/{created.Id}"));

        foreach (var name in new[] { "   ", new string('x', 151) })
        {
            using var invalid = await client.PostAsJsonAsync(route, new { name });
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            Assert.Equal("application/problem+json", invalid.Content.Headers.ContentType?.MediaType);
        }
        using var missingName = await client.PostAsJsonAsync(route, new { });
        Assert.Equal(HttpStatusCode.BadRequest, missingName.StatusCode);

        using var updatedResponse = await client.PutAsJsonAsync($"{route}/{created.Id}",
            new { name = "  Mathematics  " });
        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);
        var updated = await updatedResponse.Content.ReadFromJsonAsync<CategoryBody>();
        Assert.Equal("Mathematics", updated!.Name);

        var missingId = Guid.NewGuid();
        using var missingGet = await client.GetAsync($"{route}/{missingId}");
        Assert.Equal(HttpStatusCode.NotFound, missingGet.StatusCode);
        Assert.Equal("application/problem+json", missingGet.Content.Headers.ContentType?.MediaType);
        using var missingUpdate = await client.PutAsJsonAsync($"{route}/{missingId}", new { name = "Other" });
        Assert.Equal(HttpStatusCode.NotFound, missingUpdate.StatusCode);

        factory.Repository.UsedCategoryIds.Add(created.Id);
        using var inUse = await client.DeleteAsync($"{route}/{created.Id}");
        Assert.Equal(HttpStatusCode.Conflict, inUse.StatusCode);
        Assert.Equal("application/problem+json", inUse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(updated, await client.GetFromJsonAsync<CategoryBody>($"{route}/{created.Id}"));

        factory.Repository.UsedCategoryIds.Clear();
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.DeleteAsync($"{route}/{created.Id}")).StatusCode);
        using var missingDelete = await client.DeleteAsync($"{route}/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, missingDelete.StatusCode);
        Assert.Equal("application/problem+json", missingDelete.Content.Headers.ContentType?.MediaType);
    }

    private sealed record CategoryBody(Guid Id, string Name);

    private sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly string signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        public Guid ManagerUserId { get; } = Guid.NewGuid();
        public Guid NormalUserId { get; } = Guid.NewGuid();
        public FakeRepository Repository { get; } = new();

        public string Token(Guid userId)
        {
            var token = new JwtSecurityToken("AulaVirtual.Api", "AulaVirtual.Web",
                [new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                 new Claim("user_id", userId.ToString())],
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(signingKey)),
                    SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development")
                .UseSetting("Authentication:Jwt:Issuer", "AulaVirtual.Api")
                .UseSetting("Authentication:Jwt:Audience", "AulaVirtual.Web")
                .UseSetting("Authentication:Jwt:SigningKey", signingKey)
                .UseSetting("Storage:R2:Enabled", "false");
            builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=not_used;Username=not_used",
                    ["Storage:R2:Enabled"] = "false"
                }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICourseRepository>();
                services.RemoveAll<IPermissionChecker>();
                services.AddSingleton<ICourseRepository>(Repository);
                services.AddSingleton<IPermissionChecker>(new FakeChecker(ManagerUserId));
                services.AddSingleton<IFileStorage, UnusedStorage>();
            });
        }
    }

    private sealed class FakeChecker(Guid managerUserId) : IPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken token) =>
            Task.FromResult(userId == managerUserId && permissionCode == Permissions.CoursesManage);
    }

    private sealed class UnusedStorage : IFileStorage
    {
        public Task<StoredObject> StoreAsync(StoreFileRequest request, CancellationToken token = default) =>
            throw new NotSupportedException();
        public Task DeleteAsync(string key, FileVisibility visibility, CancellationToken token = default) =>
            throw new NotSupportedException();
        public Task<FileObjectInfo?> GetInfoAsync(string key, FileVisibility visibility,
            CancellationToken token = default) => throw new NotSupportedException();
        public Uri GetPublicUrl(string key) => throw new NotSupportedException();
        public Task<Uri> GetPrivateDownloadUrlAsync(string key, TimeSpan expiresIn,
            CancellationToken token = default) => throw new NotSupportedException();
        public Task<DirectUploadGrant> CreateDirectUploadAsync(DirectUploadRequest request,
            CancellationToken token = default) => throw new NotSupportedException();
    }

    private sealed class FakeRepository : ICourseRepository
    {
        private readonly List<CourseCategory> categories = [];
        public HashSet<Guid> UsedCategoryIds { get; } = [];

        public Task<Course?> GetAsync(Guid id, CancellationToken token) => Task.FromResult<Course?>(null);
        public Task<IReadOnlyList<Course>> ListAsync(CancellationToken token) =>
            Task.FromResult<IReadOnlyList<Course>>([]);
        public Task<bool> CategoryExistsAsync(Guid id, CancellationToken token) =>
            Task.FromResult(categories.Any(category => category.Id == id));
        public Task<CourseCategory?> GetCategoryAsync(Guid id, CancellationToken token) =>
            Task.FromResult(categories.SingleOrDefault(category => category.Id == id));
        public Task<IReadOnlyList<CourseCategory>> ListCategoriesAsync(CancellationToken token) =>
            Task.FromResult<IReadOnlyList<CourseCategory>>(categories.OrderBy(category => category.Name).ToArray());
        public Task<bool> CategoryHasCoursesAsync(Guid id, CancellationToken token) =>
            Task.FromResult(UsedCategoryIds.Contains(id));
        public void Add(Course course) => throw new NotSupportedException();
        public void AddCategory(CourseCategory category) => categories.Add(category);
        public Task<bool> DeleteCategoryAsync(CourseCategory category, CancellationToken token) =>
            Task.FromResult(categories.Remove(category));
        public Task SaveChangesAsync(CancellationToken token) => Task.CompletedTask;
    }
}
