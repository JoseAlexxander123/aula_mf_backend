using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AulaVirtual.Api.Public;

internal static class PublicApiRateLimiting
{
    public const string PolicyName = "public-api";

    public static IServiceCollection AddPublicApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue("PublicApi:RateLimit:PermitLimit", 60);
        var windowSeconds = configuration.GetValue("PublicApi:RateLimit:WindowSeconds", 60);

        if (permitLimit < 1 || windowSeconds < 1)
        {
            throw new InvalidOperationException("Public API rate limit values must be positive.");
        }

        services.AddRateLimiter(options =>
        {
            options.AddPolicy(PolicyName, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromSeconds(windowSeconds),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;
                httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                var problemDetailsService = httpContext.RequestServices
                    .GetRequiredService<IProblemDetailsService>();

                await problemDetailsService.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = httpContext,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests.",
                        Instance = httpContext.Request.Path
                    }
                });
            };
        });

        return services;
    }
}
