using FluentValidation;
using Taleem.Application.DTOs;

namespace Taleem.Application.Validation;

public sealed class CreateBookRequestValidator : AbstractValidator<CreateBookRequest>
{
    public CreateBookRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DefaultLanguage).NotEmpty().MaximumLength(16);
        RuleFor(x => x.Subtitle).MaximumLength(500);
        RuleFor(x => x.Author).MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(5000);
        RuleFor(x => x.Slug).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Slug));
    }
}
