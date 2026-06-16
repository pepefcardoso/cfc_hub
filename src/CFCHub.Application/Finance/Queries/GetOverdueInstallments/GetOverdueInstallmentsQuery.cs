using System;
using CFCHub.Domain.Shared;
using MediatR;

namespace CFCHub.Application.Finance.Queries.GetOverdueInstallments;

public record OverdueInstallmentDto(Guid Id, Guid EnrollmentId, decimal Amount, string Currency, DateOnly DueDate);

public record GetOverdueInstallmentsQuery(string? Cursor, int Limit = 20) : IRequest<PagedResult<OverdueInstallmentDto>>;
