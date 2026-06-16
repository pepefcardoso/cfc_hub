using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Finance;
using MediatR;

namespace CFCHub.Application.Finance.Queries.GetPaymentPlan;

public class GetPaymentPlanQueryHandler : IRequestHandler<GetPaymentPlanQuery, PaymentPlanResult>
{
    private readonly IInstallmentRepository _installmentRepository;

    public GetPaymentPlanQueryHandler(IInstallmentRepository installmentRepository)
    {
        _installmentRepository = installmentRepository;
    }

    public async Task<PaymentPlanResult> Handle(GetPaymentPlanQuery request, CancellationToken cancellationToken)
    {
        var installments = await _installmentRepository.GetByEnrollmentIdAsync(new EnrollmentId(request.EnrollmentId), cancellationToken);
        
        var dtos = installments.Select(i => new InstallmentDto(
            i.Id.Value,
            i.Amount.Amount,
            i.Amount.Currency,
            i.DueDate,
            i.Status.ToString()
        ));

        return new PaymentPlanResult(request.EnrollmentId, dtos);
    }
}
