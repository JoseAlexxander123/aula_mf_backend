using AulaVirtual.Modules.Auth.Application.Abstractions;
using AulaVirtual.Modules.Auth.Application.Accounts.CreateAccount;
using AulaVirtual.Modules.Auth.Domain.Accounts;
using AulaVirtual.Modules.Auth.Infrastructure.Persistence;
using AulaVirtual.Modules.Auth.Infrastructure.Security;
using AulaVirtual.Modules.Users.Application.Abstractions;
using AulaVirtual.Modules.Users.Application.Users.CreateUser;
using AulaVirtual.Modules.Users.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using AuthHistoryRepository = AulaVirtual.Modules.Auth.Infrastructure.Persistence.SnakeCaseNpgsqlHistoryRepository;
using UsersHistoryRepository = AulaVirtual.Modules.Users.Infrastructure.Persistence.SnakeCaseNpgsqlHistoryRepository;

const string confirmationFlag = "--confirm-development-provisioning";
const string requiredDatabase = "aula_virtual_dev";
const string corporateEmail = "corporate.e2e@example.com";
const string kidsDocumentNumber = "99000001";
const string kidsExpectedUsername = "P99000001";

if (!args.Contains(confirmationFlag, StringComparer.Ordinal))
{
    Console.Error.WriteLine(
        $"Explicit confirmation is required. Pass {confirmationFlag}.");
    return 2;
}

var builder = Host.CreateApplicationBuilder(
    new HostApplicationBuilderSettings
    {
        Args = [],
        ApplicationName = typeof(Program).Assembly.GetName().Name
    });

if (!builder.Environment.IsDevelopment())
{
    Console.Error.WriteLine(
        "Provisioning is allowed only when DOTNET_ENVIRONMENT=Development.");
    return 3;
}

var connectionString = builder.Configuration.GetConnectionString(
    "DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine(
        "ConnectionStrings:DefaultConnection must be supplied externally.");
    return 4;
}

var connection = new NpgsqlConnectionStringBuilder(connectionString);

if (!string.Equals(
        connection.Database,
        requiredDatabase,
        StringComparison.Ordinal))
{
    Console.Error.WriteLine(
        $"Provisioning is restricted to database '{requiredDatabase}'.");
    return 5;
}

if (!IsLocalHost(connection.Host))
{
    Console.Error.WriteLine(
        "Provisioning is restricted to a local PostgreSQL host.");
    return 6;
}

var passwordHashingSettings = builder.Configuration
    .GetSection(PasswordHashingSettings.SectionName)
    .Get<PasswordHashingSettings>() ?? new PasswordHashingSettings();

builder.Services.AddSingleton(passwordHashingSettings);
builder.Services.TryAddSingleton(TimeProvider.System);

builder.Services.AddScoped<IValidator<CreateUserCommand>, CreateUserCommandValidator>();
builder.Services.AddScoped<ICreateUser, CreateUserHandler>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<
    IValidator<CreateAdministratorAccountCommand>,
    CreateAdministratorAccountCommandValidator>();
builder.Services.AddScoped<
    IValidator<CreateStudentAccountCommand>,
    CreateStudentAccountCommandValidator>();
builder.Services.AddScoped<CreateAdministratorAccountHandler>();
builder.Services.AddScoped<CreateStudentAccountHandler>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddSingleton<IPasswordGenerator, CryptographicPasswordGenerator>();
builder.Services.AddSingleton<IPasswordHasher, Argon2idPasswordHasher>();

builder.Services.AddDbContext<UsersDbContext>(options =>
    options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable(
                "__ef_migrations_history",
                "users"))
        .ReplaceService<IHistoryRepository, UsersHistoryRepository>());
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable(
                "__ef_migrations_history",
                "auth"))
        .ReplaceService<IHistoryRepository, AuthHistoryRepository>());

using var host = builder.Build();

if (!await DatabaseIsReadyAsync(host.Services))
{
    return 7;
}

CreateAccountResult corporateResult;
CreateAccountResult kidsResult;

using (var scope = host.Services.CreateScope())
{
    var authRepository = scope.ServiceProvider
        .GetRequiredService<IAuthRepository>();
    var userRepository = scope.ServiceProvider
        .GetRequiredService<IUserRepository>();

    if (await authRepository.FindAccountByIdentifierAsync(
            LoginType.Email,
            corporateEmail.ToUpperInvariant(),
            CancellationToken.None) is not null ||
        await authRepository.FindAccountByIdentifierAsync(
            LoginType.Username,
            kidsExpectedUsername,
            CancellationToken.None) is not null ||
        await userRepository.DocumentNumberExistsAsync(
            kidsDocumentNumber,
            CancellationToken.None))
    {
        Console.Error.WriteLine(
            "One or more development test identities already exist. No accounts were created.");
        return 8;
    }

    // This existing use case creates a LoginType.Email account only. It does
    // not assign a role, permission, is_admin flag, or bootstrap capability.
    corporateResult = await scope.ServiceProvider
        .GetRequiredService<CreateAdministratorAccountHandler>()
        .ExecuteAsync(
            new CreateAdministratorAccountCommand(
                "Corporate E2E",
                "Prueba",
                null,
                corporateEmail,
                null,
                null),
            CancellationToken.None);

    if (corporateResult.Status != CreateAccountStatus.Created)
    {
        Console.Error.WriteLine(
            $"Corporate account creation failed: {corporateResult.Status}.");
        return 9;
    }

    kidsResult = await scope.ServiceProvider
        .GetRequiredService<CreateStudentAccountHandler>()
        .ExecuteAsync(
            new CreateStudentAccountCommand(
                "Kids E2E",
                "Prueba",
                null,
                null,
                "DNI",
                kidsDocumentNumber),
            CancellationToken.None);

    if (kidsResult.Status != CreateAccountStatus.Created)
    {
        Console.Error.WriteLine(
            $"Kids account creation failed: {kidsResult.Status}. The Corporate account was already persisted.");
        return 10;
    }
}

if (!await AccountsWerePersistedAsync(
        host.Services,
        corporateResult,
        kidsResult))
{
    Console.Error.WriteLine(
        "Provisioned accounts could not be verified after persistence.");
    return 11;
}

Console.WriteLine("Development test accounts provisioned and verified.");
Console.WriteLine("Corporate (LoginType.Email, Status.Active, no role):");
Console.WriteLine($"  Email: {corporateResult.LoginIdentifier}");
Console.WriteLine($"  Password: {corporateResult.InitialPassword}");
Console.WriteLine("Kids (LoginType.Username, Status.Active):");
Console.WriteLine($"  Username: {kidsResult.LoginIdentifier}");
Console.WriteLine($"  Password: {kidsResult.InitialPassword}");
Console.WriteLine("Passwords were shown once and only Argon2id hashes were persisted.");

return 0;

static bool IsLocalHost(string? host)
{
    return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(host, "127.0.0.1", StringComparison.Ordinal) ||
        string.Equals(host, "::1", StringComparison.Ordinal);
}

static async Task<bool> DatabaseIsReadyAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var usersDbContext = scope.ServiceProvider
        .GetRequiredService<UsersDbContext>();
    var authDbContext = scope.ServiceProvider
        .GetRequiredService<AuthDbContext>();

    if (!await usersDbContext.Database.CanConnectAsync() ||
        !await authDbContext.Database.CanConnectAsync())
    {
        Console.Error.WriteLine(
            "The local development database is not reachable.");
        return false;
    }

    var usersPendingMigrations = await usersDbContext.Database
        .GetPendingMigrationsAsync();
    var authPendingMigrations = await authDbContext.Database
        .GetPendingMigrationsAsync();

    if (usersPendingMigrations.Any() || authPendingMigrations.Any())
    {
        Console.Error.WriteLine(
            "Pending Users or Auth migrations were detected. This tool never applies migrations.");
        return false;
    }

    return true;
}

static async Task<bool> AccountsWerePersistedAsync(
    IServiceProvider services,
    CreateAccountResult corporateResult,
    CreateAccountResult kidsResult)
{
    using var scope = services.CreateScope();
    var usersDbContext = scope.ServiceProvider
        .GetRequiredService<UsersDbContext>();
    var authDbContext = scope.ServiceProvider
        .GetRequiredService<AuthDbContext>();

    var userIds = new[]
    {
        corporateResult.UserId!.Value,
        kidsResult.UserId!.Value
    };
    var accountIds = new[]
    {
        corporateResult.AuthAccountId!.Value,
        kidsResult.AuthAccountId!.Value
    };
    var users = await usersDbContext.Users
        .AsNoTracking()
        .Where(user => userIds.Contains(user.Id))
        .ToListAsync();
    var accounts = await authDbContext.AuthAccounts
        .AsNoTracking()
        .Include(account => account.Identifiers)
        .Where(account => accountIds.Contains(account.Id))
        .ToListAsync();

    if (users.Count != 2 || accounts.Count != 2)
    {
        return false;
    }

    var corporateAccount = accounts.Single(
        account => account.Id == corporateResult.AuthAccountId);
    var kidsAccount = accounts.Single(
        account => account.Id == kidsResult.AuthAccountId);

    var corporateIdentifier = corporateAccount.Identifiers.SingleOrDefault();
    var kidsIdentifier = kidsAccount.Identifiers.SingleOrDefault();

    return corporateIdentifier?.LoginType == LoginType.Email &&
        corporateAccount.Status == AccountStatus.Active &&
        corporateIdentifier.LoginIdentifier == corporateEmail &&
        corporateAccount.PasswordHash.StartsWith(
            "$argon2id$",
            StringComparison.Ordinal) &&
        corporateAccount.PasswordHash != corporateResult.InitialPassword &&
        kidsIdentifier?.LoginType == LoginType.Username &&
        kidsAccount.Status == AccountStatus.Active &&
        kidsIdentifier.LoginIdentifier == kidsExpectedUsername &&
        kidsAccount.PasswordHash.StartsWith(
            "$argon2id$",
            StringComparison.Ordinal) &&
        kidsAccount.PasswordHash != kidsResult.InitialPassword;
}
