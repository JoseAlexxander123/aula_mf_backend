using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
using Microsoft.Extensions.Logging;
using Npgsql;
using AuthHistoryRepository = AulaVirtual.Modules.Auth.Infrastructure.Persistence.SnakeCaseNpgsqlHistoryRepository;
using UsersHistoryRepository = AulaVirtual.Modules.Users.Infrastructure.Persistence.SnakeCaseNpgsqlHistoryRepository;

const string confirmationFlag = "--confirm-staging-provisioning";
const string expectedHost = "pg8001.site4now.net";
const int expectedPort = 6432;
const string expectedDatabase = "db_acb18a_vclass";
const string expectedUser = "acb18a_vclass";
const string kidsDocumentNumber = "STG-KIDS-0001";
const string kidsUsername = "PSTG-KIDS-0001";
const string corporateEmail = "corporate.staging.qa@example.com";

if (args.Length != 1 || args[0] != confirmationFlag)
{
    Console.Error.WriteLine($"Explicit confirmation is required: {confirmationFlag}.");
    return 2;
}

if (!string.Equals(
        Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"),
        Environments.Staging,
        StringComparison.Ordinal))
{
    Console.Error.WriteLine("DOTNET_ENVIRONMENT must be Staging.");
    return 3;
}

if (!OperatingSystem.IsWindows())
{
    Console.Error.WriteLine("This tool requires Windows DPAPI.");
    return 4;
}

var builder = Host.CreateApplicationBuilder(
    new HostApplicationBuilderSettings
    {
        Args = [],
        ApplicationName = typeof(Program).Assembly.GetName().Name
    });

// Keep SQL and parameters out of the console during this one-time operation.
builder.Logging.ClearProviders();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("DefaultConnection must be supplied externally.");
    return 5;
}

NpgsqlConnectionStringBuilder target;
try
{
    target = new NpgsqlConnectionStringBuilder(connectionString);
}
catch (ArgumentException)
{
    Console.Error.WriteLine("DefaultConnection is invalid.");
    return 5;
}

if (!string.Equals(target.Host, expectedHost, StringComparison.OrdinalIgnoreCase) ||
    target.Port != expectedPort ||
    !string.Equals(target.Database, expectedDatabase, StringComparison.Ordinal) ||
    !string.Equals(target.Username, expectedUser, StringComparison.Ordinal) ||
    target.SslMode is not (SslMode.Require or SslMode.VerifyCA or SslMode.VerifyFull))
{
    Console.Error.WriteLine("Connection target or SSL mode is not the authorized Staging target.");
    return 6;
}

var credentialsPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "AulaVirtual",
    "staging-provisioning",
    "test-accounts.dpapi");

if (File.Exists(credentialsPath))
{
    Console.Error.WriteLine("An encrypted Staging credentials file already exists. No changes made.");
    return 7;
}

builder.Services.AddSingleton(
    builder.Configuration
        .GetSection(PasswordHashingSettings.SectionName)
        .Get<PasswordHashingSettings>() ?? new PasswordHashingSettings());
builder.Services.TryAddSingleton(TimeProvider.System);
builder.Services.AddScoped<IValidator<CreateUserCommand>, CreateUserCommandValidator>();
builder.Services.AddScoped<ICreateUser, CreateUserHandler>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<
    IValidator<CreateStudentAccountCommand>,
    CreateStudentAccountCommandValidator>();
builder.Services.AddScoped<
    IValidator<CreateAdministratorAccountCommand>,
    CreateAdministratorAccountCommandValidator>();
builder.Services.AddScoped<CreateStudentAccountHandler>();
builder.Services.AddScoped<CreateAdministratorAccountHandler>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddSingleton<IPasswordGenerator, CryptographicPasswordGenerator>();
builder.Services.AddSingleton<IPasswordHasher, Argon2idPasswordHasher>();
builder.Services.AddDbContext<UsersDbContext>(options =>
    options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "users"))
        .ReplaceService<IHistoryRepository, UsersHistoryRepository>());
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "auth"))
        .ReplaceService<IHistoryRepository, AuthHistoryRepository>());

var databaseWriteStarted = false;
try
{
    await using (var connection = new NpgsqlConnection(connectionString))
    {
        await connection.OpenAsync();
        // SslMode.Require (or stronger) above makes OpenAsync fail without TLS.
        await using var identityCommand = new NpgsqlCommand(
            "SELECT current_database(), current_user",
            connection);
        await using var reader = await identityCommand.ExecuteReaderAsync();
        if (!await reader.ReadAsync() ||
            !string.Equals(reader.GetString(0), expectedDatabase, StringComparison.Ordinal) ||
            !string.Equals(reader.GetString(1), expectedUser, StringComparison.Ordinal))
        {
            Console.Error.WriteLine("The effective database identity is not the authorized target.");
            return 9;
        }
    }

    using var host = builder.Build();
    using var scope = host.Services.CreateScope();
    var services = scope.ServiceProvider;
    var usersDb = services.GetRequiredService<UsersDbContext>();
    var authDb = services.GetRequiredService<AuthDbContext>();
    var usersMigrations = (await usersDb.Database.GetAppliedMigrationsAsync()).ToArray();
    var authMigrations = (await authDb.Database.GetAppliedMigrationsAsync()).ToArray();
    if (!usersMigrations.SequenceEqual(["20260925064227_InitialUsers"]) ||
        !authMigrations.SequenceEqual([
            "20260925064556_InitialAuth",
            "20260926030030_AddAuthIdentifiers"]) ||
        (await usersDb.Database.GetPendingMigrationsAsync()).Any() ||
        (await authDb.Database.GetPendingMigrationsAsync()).Any())
    {
        Console.Error.WriteLine("Users or Auth migrations are not at the expected version.");
        return 10;
    }

    if (await usersDb.Users.AnyAsync() ||
        await authDb.AuthAccounts.AnyAsync() ||
        await authDb.AuthIdentifiers.AnyAsync() ||
        await authDb.AuthSessions.AnyAsync() ||
        await authDb.RefreshTokens.AnyAsync())
    {
        Console.Error.WriteLine("Staging contains account or session data. No changes made.");
        return 11;
    }

    var authRepository = services.GetRequiredService<IAuthRepository>();
    var userRepository = services.GetRequiredService<IUserRepository>();
    if (await authRepository.FindAccountByIdentifierAsync(
            LoginType.Username, kidsUsername, CancellationToken.None) is not null ||
        await authRepository.FindAccountByIdentifierAsync(
            LoginType.Email, corporateEmail.ToUpperInvariant(), CancellationToken.None) is not null ||
        await userRepository.DocumentNumberExistsAsync(
            kidsDocumentNumber, CancellationToken.None))
    {
        Console.Error.WriteLine("A proposed Staging identity already exists. No changes made.");
        return 12;
    }

    Directory.CreateDirectory(Path.GetDirectoryName(credentialsPath)!);
    var credentials = new List<TestCredential>();
    SaveEncryptedCredentials(credentialsPath, credentials, overwrite: false);

    databaseWriteStarted = true;
    var kidsResult = await services.GetRequiredService<CreateStudentAccountHandler>()
        .ExecuteAsync(new CreateStudentAccountCommand(
            "Kids Staging", "Prueba", null, null, "TEST", kidsDocumentNumber),
            CancellationToken.None);
    if (kidsResult.Status != CreateAccountStatus.Created)
    {
        Console.Error.WriteLine($"Kids creation stopped: {kidsResult.Status}. Inspect Staging before any retry.");
        return 13;
    }

    credentials.Add(ToCredential("Kids", kidsResult));
    SaveEncryptedCredentials(credentialsPath, credentials, overwrite: true);

    var corporateResult = await services.GetRequiredService<CreateAdministratorAccountHandler>()
        .ExecuteAsync(new CreateAdministratorAccountCommand(
            "Corporate Staging", "Prueba", null, corporateEmail, null, null),
            CancellationToken.None);
    if (corporateResult.Status != CreateAccountStatus.Created)
    {
        Console.Error.WriteLine($"Corporate creation stopped: {corporateResult.Status}. Inspect Staging before any retry.");
        return 14;
    }

    credentials.Add(ToCredential("Corporate", corporateResult));
    SaveEncryptedCredentials(credentialsPath, credentials, overwrite: true);

    if (!await ValidateResultAsync(usersDb, authDb, services.GetRequiredService<IPasswordHasher>(), credentials))
    {
        Console.Error.WriteLine("Post-provisioning validation failed. Inspect Staging before any retry.");
        return 15;
    }

    Console.WriteLine("Staging test accounts created and validated: 2 Users, 2 AuthAccounts, 2 AuthIdentifiers, 0 AuthSessions.");
    Console.WriteLine($"Encrypted credentials: {credentialsPath}");
    return 0;
}
catch (Exception)
{
    Console.Error.WriteLine(databaseWriteStarted
        ? "Provisioning stopped after a database write may have started. Inspect Staging; do not retry or clean up automatically."
        : "Provisioning preflight failed. No database write was attempted.");
    Console.Error.WriteLine($"Encrypted credentials location: {credentialsPath}");
    return 16;
}

static TestCredential ToCredential(string accountType, CreateAccountResult result)
{
    if (result.UserId is null || result.AuthAccountId is null ||
        string.IsNullOrEmpty(result.LoginIdentifier) ||
        string.IsNullOrEmpty(result.InitialPassword))
    {
        throw new InvalidOperationException("A created account has an incomplete result.");
    }

    return new TestCredential(
        accountType,
        result.UserId.Value,
        result.AuthAccountId.Value,
        result.LoginIdentifier,
        result.InitialPassword);
}

static void SaveEncryptedCredentials(
    string path,
    IReadOnlyList<TestCredential> credentials,
    bool overwrite)
{
    var plain = JsonSerializer.SerializeToUtf8Bytes(credentials);
    try
    {
        var encrypted = ProtectedData.Protect(
            plain,
            optionalEntropy: null,
            DataProtectionScope.CurrentUser);
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var file = new FileStream(
                temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                file.Write(encrypted);
                file.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, path, overwrite);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            CryptographicOperations.ZeroMemory(encrypted);
        }
    }
    finally
    {
        CryptographicOperations.ZeroMemory(plain);
    }
}

static async Task<bool> ValidateResultAsync(
    UsersDbContext usersDb,
    AuthDbContext authDb,
    IPasswordHasher passwordHasher,
    IReadOnlyList<TestCredential> credentials)
{
    var users = await usersDb.Users.AsNoTracking().ToListAsync();
    var accounts = await authDb.AuthAccounts
        .AsNoTracking()
        .Include(account => account.Identifiers)
        .ToListAsync();
    if (users.Count != 2 || accounts.Count != 2 ||
        await authDb.AuthIdentifiers.CountAsync() != 2 ||
        await authDb.AuthSessions.AnyAsync() ||
        await authDb.RefreshTokens.AnyAsync() ||
        credentials.Count != 2 ||
        accounts.Select(account => account.UserId).Distinct().Count() != 2 ||
        accounts.SelectMany(account => account.Identifiers)
            .Select(identifier => (identifier.LoginType, identifier.NormalizedLoginIdentifier))
            .Distinct().Count() != 2)
    {
        return false;
    }

    foreach (var credential in credentials)
    {
        var user = users.SingleOrDefault(candidate => candidate.Id == credential.UserId);
        var account = accounts.SingleOrDefault(candidate => candidate.Id == credential.AuthAccountId);
        if (user is null || account is null || account.UserId != user.Id ||
            account.Status != AccountStatus.Active || account.Identifiers.Count != 1 ||
            !account.PasswordHash.StartsWith("$argon2id$", StringComparison.Ordinal) ||
            !passwordHasher.Verify(credential.Password, account.PasswordHash))
        {
            return false;
        }

        var identifier = account.Identifiers.Single();
        if (credential.AccountType == "Kids")
        {
            if (user.GivenNames != "Kids Staging" || user.FirstSurname != "Prueba" ||
                user.DocumentType != "TEST" || user.DocumentNumber != kidsDocumentNumber ||
                user.Email is not null || identifier.LoginType != LoginType.Username ||
                identifier.LoginIdentifier != kidsUsername)
            {
                return false;
            }
        }
        else if (credential.AccountType == "Corporate")
        {
            if (user.GivenNames != "Corporate Staging" || user.FirstSurname != "Prueba" ||
                user.DocumentType is not null || user.DocumentNumber is not null ||
                user.Email != corporateEmail || identifier.LoginType != LoginType.Email ||
                identifier.LoginIdentifier != corporateEmail)
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    return true;
}

internal sealed record TestCredential(
    string AccountType,
    Guid UserId,
    Guid AuthAccountId,
    string LoginIdentifier,
    string Password);
