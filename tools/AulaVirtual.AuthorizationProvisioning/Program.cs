using AulaVirtual.Modules.Authorization.Api;
using AulaVirtual.Modules.Authorization.Application;
using AulaVirtual.Modules.Users.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

const string confirmation = "--confirm-authorization-change";
if (args.Length < 2 || args[^1] != confirmation ||
    (args[0] != "provision-catalog" && args[0] != "assign-first-administrator"))
{
    Console.Error.WriteLine("Usage: provision-catalog --confirm-authorization-change | assign-first-administrator <user-id> --confirm-authorization-change");
    return 2;
}

Guid userId = default;
if (args[0] == "assign-first-administrator" &&
    (args.Length != 3 || !Guid.TryParse(args[1], out userId) || userId == Guid.Empty))
{
    Console.Error.WriteLine("An explicit nonempty UserId is required.");
    return 2;
}
if (args[0] == "provision-catalog" && args.Length != 2) return 2;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [] });
if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
{
    Console.Error.WriteLine("ConnectionStrings:DefaultConnection must be supplied externally.");
    return 3;
}
builder.Services.AddUsersModule(builder.Configuration).AddAuthorizationModule(builder.Configuration);
using var host = builder.Build();
using var scope = host.Services.CreateScope();
if (args[0] == "provision-catalog")
{
    await scope.ServiceProvider.GetRequiredService<CatalogProvisioner>().ProvisionAsync(CancellationToken.None);
    Console.WriteLine($"Authorization catalog v{CatalogProvisioner.CurrentVersion} provisioned.");
}
else
{
    var assigned = await scope.ServiceProvider.GetRequiredService<AuthorizationUseCases>()
        .AssignFirstAdministratorAsync(userId, CancellationToken.None);
    Console.WriteLine(assigned ? "First administrator role assigned." : "Initial administrator bootstrap was already completed.");
}
return 0;
