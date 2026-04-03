using FluentValidation;
using Finance.Application.Records;

namespace Finance.Application.Validation;

public sealed class UpdateFinancialRecordRequestValidator : AbstractValidator<UpdateFinancialRecordRequest>
{
    public UpdateFinancialRecordRequestValidator()
    {
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);

        RuleFor(x => x)
            .Must(x => x.Amount.HasValue || x.Type.HasValue || x.Category is not null || x.RecordDate.HasValue || x.Notes is not null)
            .WithMessage("Provide at least one field to update.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .LessThanOrEqualTo(999_999_999.99m)
            .When(x => x.Amount.HasValue);

        RuleFor(x => x.Category)
            .NotEmpty()
            .MaximumLength(100)
            .When(x => x.Category is not null);

        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes is not null);
    }
}
