using FluentValidation;
using IvanovItog.Domain.Entities;

namespace IvanovItog.Infrastructure.Validation;

public class RequestValidator : AbstractValidator<Request>
{
    public RequestValidator()
    {
        RuleFor(r => r.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(r => r.Description)
            .NotEmpty()
            .MaximumLength(4000);

        RuleFor(r => r.CategoryId)
            .GreaterThan(0);

        RuleFor(r => r.StatusId)
            .GreaterThan(0);

        RuleFor(r => r.CreatedById)
            .GreaterThan(0);

        RuleFor(r => r.AssignedToId)
            .GreaterThan(0)
            .When(r => r.AssignedToId.HasValue);
    }
}
