using FluentValidation;
using Taleem.Application.DTOs;

namespace Taleem.Application.Validation;

public sealed class SemanticSearchRequestValidator : AbstractValidator<SemanticSearchRequest>
{
    public SemanticSearchRequestValidator()
    {
        RuleFor(x => x.Query).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Limit).InclusiveBetween(1, 50);
    }
}
