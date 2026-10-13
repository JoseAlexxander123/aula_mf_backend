using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AulaVirtual.Modules.Enrollments.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddEnrollmentsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return services;
    }
}
