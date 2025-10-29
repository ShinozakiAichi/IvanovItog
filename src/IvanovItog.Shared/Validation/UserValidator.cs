using FluentValidation;
using IvanovItog.Domain.Entities;

namespace IvanovItog.Shared.Validation;

public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(u => u.Login)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(100);

        RuleFor(u => u.DisplayName)
            .NotEmpty()
            .MaximumLength(150);
    }
}
