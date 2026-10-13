using FluentValidation;

namespace AulaVirtual.Modules.Users.Application.Users.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
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
            .MaximumLength(32);

        RuleFor(command => command.DocumentNumber)
            .MaximumLength(50);
    }
}
