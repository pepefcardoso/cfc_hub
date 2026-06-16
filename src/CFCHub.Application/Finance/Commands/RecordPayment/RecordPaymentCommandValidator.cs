using FluentValidation;

namespace CFCHub.Application.Finance.Commands.RecordPayment;

public class RecordPaymentCommandValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.EnrollmentId).NotEmpty();
        RuleFor(x => x.InstallmentId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Method).NotEmpty();
    }
}
