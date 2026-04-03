using FluentValidation;
using Finance.Application.Records;

namespace Finance.Application.Validation;

public sealed class BulkCreateFinancialRecordsRequestValidator : AbstractValidator<BulkCreateFinancialRecordsRequest>
{
    public BulkCreateFinancialRecordsRequestValidator(IValidator<CreateFinancialRecordRequest> itemValidator)
    {
        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("At least one record is required.");

        RuleFor(x => x.Items.Count)
            .LessThanOrEqualTo(BulkCreateFinancialRecordsLimits.MaxItems)
            .WithMessage($"Maximum {BulkCreateFinancialRecordsLimits.MaxItems} records per request.");

        RuleForEach(x => x.Items).SetValidator(itemValidator);
    }
}
