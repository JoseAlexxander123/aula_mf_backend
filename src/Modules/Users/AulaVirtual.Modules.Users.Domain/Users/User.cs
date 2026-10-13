namespace AulaVirtual.Modules.Users.Domain.Users;

public sealed class User
{
    private User()
    {
    }

    private User(
        Guid id,
        string givenNames,
        string firstSurname,
        string? secondSurname,
        string? email,
        string? documentType,
        string? documentNumber,
        DateTimeOffset createdAt)
    {
        Id = id;
        GivenNames = givenNames;
        FirstSurname = firstSurname;
        SecondSurname = secondSurname;
        Email = email;
        DocumentType = documentType;
        DocumentNumber = documentNumber;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string GivenNames { get; private set; } = string.Empty;

    public string FirstSurname { get; private set; } = string.Empty;

    public string? SecondSurname { get; private set; }

    public string? Email { get; private set; }

    public string? DocumentType { get; private set; }

    public string? DocumentNumber { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static User Create(
        Guid id,
        string givenNames,
        string firstSurname,
        string? secondSurname,
        string? email,
        string? documentType,
        string? documentNumber,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(givenNames);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstSurname);

        return new User(
            id,
            givenNames.Trim(),
            firstSurname.Trim(),
            TrimToNull(secondSurname),
            TrimToNull(email),
            TrimToNull(documentType),
            TrimToNull(documentNumber),
            createdAt);
    }

    private static string? TrimToNull(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
