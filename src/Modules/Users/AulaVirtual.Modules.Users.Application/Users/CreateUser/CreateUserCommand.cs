namespace AulaVirtual.Modules.Users.Application.Users.CreateUser;

public sealed record CreateUserCommand(
    string GivenNames,
    string FirstSurname,
    string? SecondSurname,
    string? Email,
    string? DocumentType,
    string? DocumentNumber);
