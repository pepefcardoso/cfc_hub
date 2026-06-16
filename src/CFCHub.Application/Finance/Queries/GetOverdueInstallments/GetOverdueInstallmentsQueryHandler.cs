using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Finance;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using MediatR;

namespace CFCHub.Application.Finance.Queries.GetOverdueInstallments;

public class GetOverdueInstallmentsQueryHandler : IRequestHandler<GetOverdueInstallmentsQuery, PagedResult<OverdueInstallmentDto>>
{
    private readonly IInstallmentRepository _installmentRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetOverdueInstallmentsQueryHandler(
        IInstallmentRepository installmentRepository,
        ICurrentUserService currentUserService)
    {
        _installmentRepository = installmentRepository;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<OverdueInstallmentDto>> Handle(GetOverdueInstallmentsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.Role != RoleType.Financial && _currentUserService.Role != RoleType.Admin)
        {
            throw new ForbiddenException("Only Financial or Admin roles can view overdue installments.");
        }

        var result = await _installmentRepository.ListOverdueAsync(request.Cursor, request.Limit, cancellationToken);
        
        var dtos = result.Items.Select(i => new OverdueInstallmentDto(
            i.Id.Value,
            i.EnrollmentId.Value,
            i.Amount.Amount,
            i.Amount.Currency,
            i.DueDate
        )).ToList();

        return new PagedResult<OverdueInstallmentDto>(dtos, result.NextCursor, result.HasMore, result.Count);
    }
}
