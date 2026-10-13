using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AulaVirtual.Modules.Certificates.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddCertificatesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return services;
    }
}
