using FluentValidation;
using Finance.Application.Records;

namespace Finance.Application.Validation;

public sealed class CreateFinancialRecordRequestValidator : AbstractValidator<CreateFinancialRecordRequest>
{
    public CreateFinancialRecordRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(999_999_999.99m);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
