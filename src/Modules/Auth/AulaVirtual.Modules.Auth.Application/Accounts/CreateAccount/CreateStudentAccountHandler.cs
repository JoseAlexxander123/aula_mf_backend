using AulaVirtual.Modules.Auth.Application.Abstractions;
using AulaVirtual.Modules.Auth.Domain.Accounts;
using AulaVirtual.Modules.Users.Application.Users.CreateUser;
using FluentValidation;

namespace AulaVirtual.Modules.Auth.Application.Accounts.CreateAccount;

public sealed class CreateStudentAccountHandler(
    ICreateUser createUser,
    IAuthRepository authRepository,
    IPasswordGenerator passwordGenerator,
    IPasswordHasher passwordHasher,
    IValidator<CreateStudentAccountCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<CreateAccountResult> ExecuteAsync(
        CreateStudentAccountCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);

        if (!validation.IsValid)
        {
            return new CreateAccountResult(
                CreateAccountStatus.Invalid,
                Errors: validation.ToDictionary());
        }

        var username = string.Concat(
            char.ToUpperInvariant(command.FirstSurname.Trim()[0]),
            command.DocumentNumber.Trim());

        var duplicate = await authRepository.FindAccountByIdentifierAsync(
            LoginType.Username,
            username.ToUpperInvariant(),
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
                command.Email,
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

        return await CreateAccountAsync(
            userResult.UserId.Value,
            LoginType.Username,
            username,
            cancellationToken);
    }

    private async Task<CreateAccountResult> CreateAccountAsync(
        Guid userId,
        LoginType loginType,
        string loginIdentifier,
        CancellationToken cancellationToken)
    {
        var password = passwordGenerator.Generate();
        var now = timeProvider.GetUtcNow();
        var account = AuthAccount.Create(
            Guid.NewGuid(),
            userId,
            passwordHasher.Hash(password),
            now);
        var identifier = account.AddIdentifier(
            Guid.NewGuid(),
            loginType,
            loginIdentifier,
            now);

        authRepository.AddAccount(account);
        await authRepository.SaveChangesAsync(cancellationToken);

        return new CreateAccountResult(
            CreateAccountStatus.Created,
            userId,
            account.Id,
            identifier.LoginIdentifier,
            password);
    }
}
