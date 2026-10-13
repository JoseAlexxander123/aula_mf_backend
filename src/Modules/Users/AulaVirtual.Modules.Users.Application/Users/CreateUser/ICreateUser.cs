namespace AulaVirtual.Modules.Users.Application.Users.CreateUser;

public interface ICreateUser
{
    Task<CreateUserResult> ExecuteAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken);
}
