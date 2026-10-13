using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using AulaVirtual.SharedKernel.Application.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AulaVirtual.Modules.Auth.Tests;

public sealed class PublicApiTests
{
    [Fact]
    public async Task Status_IsAnonymousAndExposesOnlyPublicData()
    {
        await using var factory = new PublicApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/public/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(body);
        Assert.Equal("available", body.RootElement.GetProperty("status").GetString());
        Assert.Equal(2, body.RootElement.EnumerateObject().Count());
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("correlationId").ValueKind);
    }

    [Fact]
    public async Task Status_RejectsInvalidInputAsProblemDetails()
    {
        await using var factory = new PublicApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/v1/public/status?correlationId=invalid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(body);
        Assert.Equal(400, body.RootElement.GetProperty("status").GetInt32());
        Assert.True(body.RootElement.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Status_IsRateLimitedWithProblemDetails()
    {
        await using var factory = new PublicApiFactory(permitLimit: 2);
        using var client = factory.CreateClient();

        using var first = await client.GetAsync("/api/v1/public/status");
        using var second = await client.GetAsync("/api/v1/public/status");
        using var rejected = await client.GetAsync("/api/v1/public/status");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType?.MediaType);
        using var body = await rejected.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(body);
        Assert.Equal(429, body.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task PrivateEndpoint_StillRequiresJwt()
    {
        await using var factory = new PublicApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed class PublicApiFactory(int permitLimit = 60)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("PublicApi:RateLimit:PermitLimit", permitLimit.ToString());
            builder.UseSetting("PublicApi:RateLimit:WindowSeconds", "60");
            builder.UseSetting("Storage:R2:Enabled", "false");
            builder.UseSetting(
                "Authentication:Jwt:SigningKey",
                Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
            builder.ConfigureAppConfiguration(configuration =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Host=localhost;Database=not_used;Username=not_used",
                });
            });
            builder.ConfigureTestServices(services =>
                services.AddSingleton<IFileStorage, UnusedStorage>());
        }
    }

    private sealed class UnusedStorage : IFileStorage
    {
        public Uri? GetPublicUrl(string key) => null;
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
