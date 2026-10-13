using AulaVirtual.Modules.Users.Application.Abstractions;
using AulaVirtual.Modules.Users.Domain.Users;
using FluentValidation;

namespace AulaVirtual.Modules.Users.Application.Users.CreateUser;

public sealed class CreateUserHandler(
    IUserRepository repository,
    IValidator<CreateUserCommand> validator,
    TimeProvider timeProvider) : ICreateUser
{
    public async Task<CreateUserResult> ExecuteAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);

        if (!validation.IsValid)
        {
            return new CreateUserResult(
                CreateUserStatus.Invalid,
                Errors: validation.ToDictionary());
        }

        var documentNumber = TrimToNull(command.DocumentNumber);

        if (documentNumber is not null &&
            await repository.DocumentNumberExistsAsync(
                documentNumber,
                cancellationToken))
        {
            return new CreateUserResult(CreateUserStatus.DuplicateDocument);
        }

        var user = User.Create(
            Guid.NewGuid(),
            command.GivenNames,
            command.FirstSurname,
            command.SecondSurname,
            command.Email,
            command.DocumentType,
            documentNumber,
            timeProvider.GetUtcNow());

        repository.Add(user);
        await repository.SaveChangesAsync(cancellationToken);

        return new CreateUserResult(CreateUserStatus.Created, user.Id);
    }

    private static string? TrimToNull(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
