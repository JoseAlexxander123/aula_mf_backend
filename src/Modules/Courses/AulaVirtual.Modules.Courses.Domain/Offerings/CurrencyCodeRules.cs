namespace AulaVirtual.Modules.Courses.Domain.Offerings;

public static class CurrencyCodeRules
{
    public static string Validate(string? value)
    {
        if (value is null || value.Length != 3 || value.Any(character => character is < 'A' or > 'Z'))
            throw new ArgumentException("CurrencyCode must be three uppercase Latin letters.", nameof(value));
        return value;
    }
}
