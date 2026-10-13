using FluentValidation;

namespace AulaVirtual.Modules.Auth.Application.Accounts.CreateAccount;

public sealed class CreateStudentAccountCommandValidator
    : AbstractValidator<CreateStudentAccountCommand>
{
    public CreateStudentAccountCommandValidator()
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
            .MaximumLength(254)
            .EmailAddress()
            .When(command => !string.IsNullOrWhiteSpace(command.Email));

        RuleFor(command => command.DocumentType)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(command => command.DocumentNumber)
            .NotEmpty()
            .MaximumLength(50);
    }
}
