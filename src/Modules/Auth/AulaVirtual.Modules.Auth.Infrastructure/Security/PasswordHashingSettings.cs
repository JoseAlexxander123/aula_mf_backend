namespace AulaVirtual.Modules.Auth.Infrastructure.Security;

public sealed class PasswordHashingSettings
{
    public const string SectionName = "Authentication:PasswordHashing";

    public int MemorySizeKiB { get; init; } = 65536;

    public int Iterations { get; init; } = 2;

    public int Parallelism { get; init; } = 1;

    public int SaltSizeBytes { get; init; } = 16;

    public int HashSizeBytes { get; init; } = 32;
}
