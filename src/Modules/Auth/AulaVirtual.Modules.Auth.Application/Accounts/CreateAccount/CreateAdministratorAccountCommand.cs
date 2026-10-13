namespace AulaVirtual.Modules.Auth.Application.Accounts.CreateAccount;

public sealed record CreateAdministratorAccountCommand(
    string GivenNames,
    string FirstSurname,
    string? SecondSurname,
    string Email,
    string? DocumentType,
    string? DocumentNumber);
