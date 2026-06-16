using System;
using MediatR;

namespace CFCHub.Application.Finance.Queries.GetPaymentPlan;

public record GetPaymentPlanQuery(Guid EnrollmentId) : IRequest<PaymentPlanResult>;
