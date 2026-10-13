using FluentValidation;

namespace AulaVirtual.Modules.Auth.Application.Accounts.CreateAccount;

public sealed class CreateAdministratorAccountCommandValidator
    : AbstractValidator<CreateAdministratorAccountCommand>
{
    public CreateAdministratorAccountCommandValidator()
    {
        RuleFor(command => command.GivenNames)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(command => command.FirstSurname)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.SecondSurname)
            .MaximumLength(100);

        RuleFor(command => command.Email)
            .NotEmpty()
            .MaximumLength(254)
            .EmailAddress();

        RuleFor(command => command.DocumentType)
            .MaximumLength(32);

        RuleFor(command => command.DocumentNumber)
            .MaximumLength(50);
    }
}
