using AulaVirtual.Modules.Auth.Application.Abstractions;
using AulaVirtual.Modules.Auth.Domain.Accounts;
using AulaVirtual.Modules.Users.Application.Users.CreateUser;
using FluentValidation;

namespace AulaVirtual.Modules.Auth.Application.Accounts.CreateAccount;

public sealed class CreateAdministratorAccountHandler(
    ICreateUser createUser,
    IAuthRepository authRepository,
    IPasswordGenerator passwordGenerator,
    IPasswordHasher passwordHasher,
    IValidator<CreateAdministratorAccountCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<CreateAccountResult> ExecuteAsync(
        CreateAdministratorAccountCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);

        if (!validation.IsValid)
        {
            return new CreateAccountResult(
                CreateAccountStatus.Invalid,
                Errors: validation.ToDictionary());
        }

        var email = command.Email.Trim();
        var duplicate = await authRepository.FindAccountByIdentifierAsync(
            LoginType.Email,
            email.ToUpperInvariant(),
            cancellationToken);

        if (duplicate is not null)
        {
            return new CreateAccountResult(CreateAccountStatus.DuplicateLogin);
        }

        var userResult = await createUser.ExecuteAsync(
            new CreateUserCommand(
                command.GivenNames,
                command.FirstSurname,
                command.SecondSurname,
                email,
                command.DocumentType,
                command.DocumentNumber),
            cancellationToken);

        if (userResult.Status == CreateUserStatus.DuplicateDocument)
        {
            return new CreateAccountResult(
                CreateAccountStatus.DuplicateDocument);
        }

        if (userResult.Status != CreateUserStatus.Created ||
            userResult.UserId is null)
        {
            return new CreateAccountResult(
                CreateAccountStatus.UserCreationFailed);
        }

        var password = passwordGenerator.Generate();
        var now = timeProvider.GetUtcNow();
        var account = AuthAccount.Create(
            Guid.NewGuid(),
            userResult.UserId.Value,
            passwordHasher.Hash(password),
            now);
        var identifier = account.AddIdentifier(
            Guid.NewGuid(),
            LoginType.Email,
            email,
            now);

        authRepository.AddAccount(account);
        await authRepository.SaveChangesAsync(cancellationToken);

        return new CreateAccountResult(
            CreateAccountStatus.Created,
            userResult.UserId,
            account.Id,
            identifier.LoginIdentifier,
            password);
    }
}
