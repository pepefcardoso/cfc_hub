using System;
using System.Collections.Generic;

namespace CFCHub.Application.Finance.Queries.GetPaymentPlan;

public record InstallmentDto(Guid Id, decimal Amount, string Currency, DateOnly DueDate, string Status);

public record PaymentPlanResult(Guid EnrollmentId, IEnumerable<InstallmentDto> Installments);
