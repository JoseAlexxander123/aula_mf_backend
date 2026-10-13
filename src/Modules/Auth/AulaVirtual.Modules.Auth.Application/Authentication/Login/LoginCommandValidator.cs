using AulaVirtual.Modules.Auth.Domain.Accounts;
using FluentValidation;

namespace AulaVirtual.Modules.Auth.Application.Authentication.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Identifier)
            .NotEmpty()
            .MaximumLength(254);

        RuleFor(command => command.Identifier)
            .EmailAddress()
            .When(command => command.LoginType == LoginType.Email);

        RuleFor(command => command.Password)
            .NotEmpty();
    }
}
